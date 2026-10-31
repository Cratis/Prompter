// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Rest;

namespace Cratis.Prompter.Discord;

/// <summary>
/// Answers new threads created in the configured help forum, posting a grounded answer as the first
/// reply followed by a standing note that a human will follow up. A delivered answer also lets the thread's author
/// keep asking without a mention (answered by <see cref="Mentions"/>) until another human joins in.
/// </summary>
/// <param name="rest">The REST client used to read the starter message and post replies into the thread.</param>
/// <param name="assistance">Documentation answers and separate advisory hints.</param>
/// <param name="admission">The cheap shared admission guard.</param>
/// <param name="work">The global concurrency and durable spend guard.</param>
/// <param name="context">Request-only bounded conversation history.</param>
/// <param name="followUps">Records answered threads so their author's follow-ups are answered.</param>
/// <param name="options">The Prompter options carrying the help-forum channel identifier.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class HelpForum(
    RestClient rest,
    ConversationAssistance assistance,
    Admission admission,
    PaidWork work,
    ConversationContextReader context,
    ForumFollowUps followUps,
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
    /// NetCord already catches and logs a throwing gateway handler, but it won't answer the thread or log with
    /// this handler's context — so the whole body is guarded anyway: the starter's author is throttled before
    /// the expensive answer path, answering runs under a timeout, and any failure is logged and answered
    /// in-thread with a short apology instead of leaving the thread silent.
    /// </remarks>
    public async ValueTask HandleAsync(GuildThreadCreateEventArgs arg)
    {
        try
        {
            var thread = arg.Thread;
            if (!SupportedEvent(arg))
            {
                return;
            }

            var included = (thread as ForumGuildThread)?.Message;
            if (included is not null && !HumanStarter(included))
            {
                return;
            }

            // Even a missing starter must pass metadata, rates, concurrency and spend before the REST fallback.
            var decision = admission.TryEnter(new(thread.GuildId, thread.Id, thread.OwnerId), included?.Content ?? $"forum-starter:{thread.Id}");
            if (!decision.Allowed)
            {
                await Refuse(thread, decision.Reply);
                return;
            }

            await using var lease = await work.BeginInteractive();
            lease.Activate();
            if (!await AnswerThread(thread, included, lease.Token))
            {
                return;
            }

            followUps.Answered(arg.Thread.Id);
        }
        catch (WorkRefused)
        {
            await Refuse(arg.Thread, admission.MayReply(new(arg.Thread.GuildId, arg.Thread.Id, arg.Thread.OwnerId)));

            return;
        }
        catch (Exception exception)
        {
            logger.AnswerFailed(exception, arg.Thread.Id);
            await TryApologize(arg.Thread.Id);
            return;
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

    static bool HumanStarter(RestMessage starter) =>
        !starter.Author.IsBot && starter.WebhookId is null && starter.Type is MessageType.Default or MessageType.Reply;

    bool SupportedEvent(GuildThreadCreateEventArgs arg) => arg.NewlyCreated && SupportedThread(arg.Thread);

    bool SupportedThread(GuildThread thread) =>
        options.Value.Discord.HelpForumChannelId is { } forum && thread.ParentId == forum &&
        admission.InScope(thread.GuildId, forum) && admission.InScope(thread.GuildId, thread.Id);

    async Task Refuse(GuildThread thread, bool reply)
    {
        if (reply)
        {
            await rest.SendMessageAsync(thread.Id, options.Value.Discord.RateLimitedReply);
        }
    }

    async Task<bool> AnswerThread(GuildThread thread, RestMessage? included, CancellationToken cancellationToken)
    {
        var starter = included ?? await rest.GetMessageAsync(thread.Id, thread.Id, cancellationToken: cancellationToken);
        if (!HumanStarter(starter) || starter.Content.Length > Math.Min(options.Value.Protection.MaxQuestionCharacters, ConversationContextReader.MaximumCurrentCharacters))
        {
            return false;
        }

        var question = starter.Content.Trim();
        if (!ShouldAnswer(thread.ParentId, options.Value.Discord.HelpForumChannelId, starter.Author.IsBot, question))
        {
            return false;
        }

        logger.AnsweringForumThread(thread.Id);
        var request = await context.For(question, new(thread.GuildId, thread.Id, starter.Id, starter.CreatedAt), cancellationToken);
        var reply = await assistance.For(request with { AllowClarification = true }, "discord-forum", cancellationToken);
        await SendAnswer(thread.Id, reply, new(thread.Id, starter.Id));
        var advice = DiscordAnswers.Advice(reply);
        if (advice.Length > 0)
        {
            await rest.SendMessageAsync(thread.Id, SafeMessages.Message(advice));
        }
        return true;
    }

    async Task SendAnswer(ulong thread, ConversationReply reply, QuestionLink link)
    {
        var answer = reply.Answer;
        var message = SafeMessages.Message(DiscordAnswers.Format(answer));
        var rows = AnswerComponents.For(answer, AnswerActions.For(options.Value.Discord, link, reply));
        if (rows.Count > 0)
        {
            message.Components = [.. rows];
        }
        await rest.SendMessageAsync(thread, message);
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
