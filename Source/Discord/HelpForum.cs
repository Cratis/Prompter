// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Prompter.Answering;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Rest;

namespace Cratis.Prompter.Discord;

/// <summary>
/// Answers new threads created in the configured help forum, posting a grounded answer as the first
/// reply followed by a standing note that a human will follow up.
/// </summary>
/// <param name="rest">The REST client used to read the starter message and post replies into the thread.</param>
/// <param name="answers">The answers Prompter can give.</param>
/// <param name="interactionLog">The interaction log, used to record which message the answer landed on.</param>
/// <param name="rateLimiter">The per-user question throttle, checked before the expensive answer path.</param>
/// <param name="timeProvider">The clock the rate limiter's refills are measured against.</param>
/// <param name="options">The Prompter options carrying the help-forum channel identifier.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class HelpForum(
    RestClient rest,
    IAnswers answers,
    IInteractionLog interactionLog,
    RateLimiter rateLimiter,
    TimeProvider timeProvider,
    IOptions<PrompterOptions> options,
    ILogger<HelpForum> logger) : IGuildThreadCreateGatewayHandler
{
    /// <summary>
    /// The standing note posted after every auto-reply, inviting a human follow-up and pointing at the
    /// feedback buttons on the answer.
    /// </summary>
    public const string FollowUpNote = "A human will follow up — the 👍/👎 buttons above tell us if this helped.";

    /// <summary>
    /// Decides whether a newly created thread should receive an automatic answer.
    /// </summary>
    /// <param name="parentChannelId">The identifier of the thread's parent channel, if any.</param>
    /// <param name="helpForumChannelId">
    /// The configured help-forum channel identifier, or <see langword="null"/> when the feature is unset.
    /// </param>
    /// <param name="authorIsBot">Whether the thread's starter message was written by a bot.</param>
    /// <param name="starterText">The text of the thread's starter message.</param>
    /// <returns>
    /// <see langword="true"/> when the thread lives in the configured help forum, was started by a human,
    /// and carries a non-empty question; otherwise <see langword="false"/>.
    /// </returns>
    public static bool ShouldAnswer(ulong? parentChannelId, ulong? helpForumChannelId, bool authorIsBot, string? starterText) =>
        helpForumChannelId is { } forumId &&
        parentChannelId == forumId &&
        !authorIsBot &&
        !string.IsNullOrWhiteSpace(starterText);

    /// <inheritdoc/>
    /// <remarks>
    /// A gateway handler must never throw — an unhandled exception can destabilize the gateway loop — so the
    /// whole body is guarded: the starter's author is throttled before the expensive answer path, answering
    /// runs under a timeout, and any failure is logged and answered in-thread with a short apology instead of
    /// leaving the thread silent.
    /// </remarks>
    public async ValueTask HandleAsync(GuildThreadCreateEventArgs arg)
    {
        long? auditInteractionId = null;
        string? answerMessageId = null;
        try
        {
            // Discord raises THREAD_CREATE both for brand-new threads and when the bot merely gains visibility
            // of an existing one; only genuinely new threads carry newly_created, so ignore the rest.
            if (!arg.NewlyCreated)
            {
                return;
            }

            var thread = arg.Thread;
            var helpForumChannelId = options.Value.Discord.HelpForumChannelId;

            // Cheap pre-filter to avoid a REST round-trip for threads outside the configured help forum.
            if (helpForumChannelId is not { } forumId || thread.ParentId != forumId)
            {
                return;
            }

            // A forum thread's starter message rides along on the create event; its id equals the thread id,
            // so fall back to fetching it directly if the payload did not include it.
            var starter = thread is ForumGuildThread { Message: { } forumStarter }
                ? forumStarter
                : await rest.GetMessageAsync(thread.Id, thread.Id);

            var question = starter.Content.Trim();
            if (!ShouldAnswer(thread.ParentId, helpForumChannelId, starter.Author.IsBot, question))
            {
                return;
            }

            var userHash = UserHash.For(starter.Author.Id);
            if (!rateLimiter.TryConsume(userHash, timeProvider.GetUtcNow()))
            {
                logger.RateLimited(thread.Id, starter.Author.Id);
                await rest.SendMessageAsync(thread.Id, options.Value.Discord.RateLimitedReply);
                return;
            }

            logger.AnsweringForumThread(thread.Id, starter.Author.Id);

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(options.Value.Discord.AnswerTimeoutSeconds));
            var answer = await answers.For(new(question), userHash, "discord-forum", timeout.Token);
            var content = DiscordAnswers.Format(answer);

            // The 👍/👎 feedback buttons ride on the answer message; the standing note follows as its own message.
            if (answer.InteractionId is { } interactionId)
            {
                var sent = await rest.SendMessageAsync(thread.Id, new MessageProperties
                {
                    Content = content,
                    Components = [FeedbackButtonRow.For(interactionId)]
                });

                auditInteractionId = interactionId;
                answerMessageId = sent.Id.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                await rest.SendMessageAsync(thread.Id, content);
            }
        }
        catch (Exception exception)
        {
            logger.AnswerFailed(exception, arg.Thread.Id);
            await TryApologize(arg.Thread.Id);
            return;
        }

        // The answer is delivered. Recording which message it landed on is audit-only, so its failure is
        // best-effort here and never reaches the answer-failed catch to apologize on top of a good answer.
        if (auditInteractionId is { } recordedId && answerMessageId is { } messageId)
        {
            try
            {
                await interactionLog.SetAnswerMessage(recordedId, messageId);
            }
            catch (Exception exception)
            {
                logger.AnswerMessageAuditFailed(exception, arg.Thread.Id);
            }
        }

        // The standing "a human will follow up" note is a best-effort follow-up to a delivered answer; a send
        // failure here must not trigger the answer-failed apology on top of a good answer.
        try
        {
            await rest.SendMessageAsync(arg.Thread.Id, FollowUpNote);
        }
        catch (Exception exception)
        {
            logger.FollowUpNoteFailed(exception, arg.Thread.Id);
        }
    }

    async Task TryApologize(ulong threadId)
    {
        try
        {
            await rest.SendMessageAsync(threadId, options.Value.Discord.ErrorReply);
        }
        catch (Exception exception)
        {
            // The apology itself failed to post (thread deleted, missing permission); there is nothing more to
            // do but record it — the handler still returns without throwing.
            logger.ApologyFailed(exception, threadId);
        }
    }
}
