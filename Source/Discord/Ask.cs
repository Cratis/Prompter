// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Prompter.Answering;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace Cratis.Prompter.Discord;

/// <summary>
/// The <c>/ask</c> slash command.
/// </summary>
/// <param name="answers">The answers Prompter can give.</param>
/// <param name="interactionLog">The interaction log, used to record which message the answer landed on.</param>
/// <param name="rateLimiter">The per-user question throttle, checked before deferring.</param>
/// <param name="timeProvider">The clock the rate limiter's refills are measured against.</param>
/// <param name="options">The Prompter options carrying the rate-limit window, timeout, and reply text.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class Ask(
    IAnswers answers,
    IInteractionLog interactionLog,
    RateLimiter rateLimiter,
    TimeProvider timeProvider,
    IOptions<PrompterOptions> options,
    ILogger<Ask> logger) : ApplicationCommandModule<ApplicationCommandContext>
{
    /// <summary>
    /// Answers a question about Cratis, grounded in the documentation.
    /// </summary>
    /// <param name="question">The question to answer.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// Answering takes several seconds, well beyond Discord's 3-second interaction acknowledgement
    /// window, so the command defers immediately (showing a "thinking…" state) and then delivers the
    /// answer as a followup message carrying the 👍/👎 feedback buttons. The rate limit is checked before
    /// deferring so an over-limit ask replies ephemerally without a public "thinking…" placeholder; any
    /// failure or timeout while answering lands a short apology as the followup so the placeholder never hangs.
    /// </remarks>
    [SlashCommand("ask", "Ask Prompter a question about Cratis")]
    public async Task Handle(string question)
    {
        var userHash = UserHash.For(Context.User.Id);

        // Check the throttle before deferring: the ephemeral flag is locked at acknowledgement time, so a
        // rate-limited ask must respond ephemerally here rather than after a public defer.
        if (!rateLimiter.TryConsume(userHash, timeProvider.GetUtcNow()))
        {
            logger.RateLimited(Context.User.Id);
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message(new()
                {
                    Content = options.Value.Discord.RateLimitedReply,
                    Flags = MessageFlags.Ephemeral
                }));
            return;
        }

        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage());

        long? auditInteractionId = null;
        string? answerMessageId = null;
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(options.Value.Discord.AnswerTimeoutSeconds));
            var answer = await answers.For(new(question), userHash, "discord-ask", timeout.Token);
            var content = DiscordAnswers.Format(answer);

            if (answer.InteractionId is { } interactionId)
            {
                var sent = await Context.Interaction.SendFollowupMessageAsync(new()
                {
                    Content = content,
                    Components = [FeedbackButtonRow.For(interactionId)]
                });

                auditInteractionId = interactionId;
                answerMessageId = sent.Id.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                await Context.Interaction.SendFollowupMessageAsync(content);
            }
        }
        catch (Exception exception)
        {
            // Already deferred, so the apology must land as a followup inside the interaction window,
            // resolving the "thinking…" placeholder instead of leaving the user hanging.
            logger.AnswerFailed(exception, Context.User.Id);
            await TryApologize();
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
                logger.AnswerMessageAuditFailed(exception, Context.User.Id);
            }
        }
    }

    async Task TryApologize()
    {
        try
        {
            await Context.Interaction.SendFollowupMessageAsync(options.Value.Discord.ErrorReply);
        }
        catch (Exception exception)
        {
            // The followup itself failed to send; nothing more can be delivered to the interaction, so just
            // record it — the command still returns without throwing.
            logger.ApologyFailed(exception, Context.User.Id);
        }
    }
}
