// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Rest;

namespace Cratis.Prompter.Discord;

/// <summary>
/// The shared forum answer path: admission, spend, the grounded answer, its buttons and the standing follow-up note.
/// Immediate help-forum answers and delayed answers both run through it.
/// </summary>
/// <param name="rest">The REST client used to read the starter message and post replies into the thread.</param>
/// <param name="assistance">Documentation answers and separate advisory hints.</param>
/// <param name="admission">The cheap shared admission guard.</param>
/// <param name="work">The global concurrency and durable spend guard.</param>
/// <param name="context">Request-only bounded conversation history.</param>
/// <param name="followUps">Records answered threads so their author's follow-ups are answered.</param>
/// <param name="options">The Prompter options.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class ForumAnswerer(
    RestClient rest,
    ConversationAssistance assistance,
    Admission admission,
    PaidWork work,
    ConversationContextReader context,
    ForumFollowUps followUps,
    IOptions<PrompterOptions> options,
    ILogger<ForumAnswerer> logger) : IForumAnswerer
{
    /// <inheritdoc/>
    /// <remarks>
    /// The starter's author is throttled before the expensive answer path, answering runs under a timeout, and any
    /// failure is logged and answered in-thread with a short apology instead of leaving the thread silent.
    /// </remarks>
    public async Task Answer(ForumThreadRef thread, ulong forumId, RestMessage? included, bool delayed)
    {
        try
        {
            if (!await TryAnswer(thread, forumId, included, delayed))
            {
                return;
            }
        }
        catch (WorkRefused)
        {
            await Refuse(thread.Id, admission.MayReply(new(thread.GuildId, thread.Id, thread.OwnerId)));

            return;
        }
        catch (Exception exception)
        {
            logger.AnswerFailed(exception, thread.Id);
            await TryApologize(thread.Id);
            return;
        }

        // The standing "a human will follow up" note is a best-effort follow-up to a delivered answer; a send
        // failure here must not trigger the answer-failed apology on top of a good answer.
        try
        {
            await rest.SendMessageAsync(thread.Id, SafeMessages.Message(HelpForum.FollowUpNote));
        }
        catch (Exception exception)
        {
            logger.FollowUpNoteFailed(exception, thread.Id);
        }
    }

    static bool HumanStarter(RestMessage starter) =>
        !starter.Author.IsBot && starter.WebhookId is null && starter.Type is MessageType.Default or MessageType.Reply;

    async Task<bool> TryAnswer(ForumThreadRef thread, ulong forumId, RestMessage? included, bool delayed)
    {
        if (included is not null && !HumanStarter(included))
        {
            return false;
        }

        // Even a missing starter must pass metadata, rates, concurrency and spend before the REST fallback.
        var decision = admission.TryEnter(new(thread.GuildId, thread.Id, thread.OwnerId), included?.Content ?? $"forum-starter:{thread.Id}");
        if (!decision.Allowed)
        {
            await Refuse(thread.Id, decision.Reply, decision.Text);
            return false;
        }

        await using var lease = await work.BeginInteractive();
        lease.Activate();
        if (!await AnswerThread(thread, forumId, included, delayed, lease.Token))
        {
            return false;
        }

        TrackFollowUps(thread, delayed);
        return true;
    }

    void TrackFollowUps(ForumThreadRef thread, bool delayed)
    {
        if (delayed)
        {
            followUps.Opened(thread.Id, thread.OwnerId);
        }
        else
        {
            followUps.Answered(thread.Id);
        }
    }

    async Task Refuse(ulong threadId, bool reply, string? text = null)
    {
        if (reply)
        {
            await rest.SendMessageAsync(threadId, SafeMessages.Message(text ?? options.Value.Discord.RateLimitedReply));
        }
    }

    async Task<bool> AnswerThread(ForumThreadRef thread, ulong forumId, RestMessage? included, bool delayed, CancellationToken cancellationToken)
    {
        var starter = included ?? await rest.GetMessageAsync(thread.Id, thread.Id, cancellationToken: cancellationToken);
        if (!HumanStarter(starter) || starter.Content.Length > Math.Min(options.Value.Protection.MaxQuestionCharacters, ConversationContextReader.MaximumCurrentCharacters))
        {
            return false;
        }

        var question = starter.Content.Trim();
        if (!HelpForum.ShouldAnswer(thread.ParentId, forumId, starter.Author.IsBot, question))
        {
            return false;
        }

        logger.AnsweringForumThread(thread.Id);
        var request = await context.For(question, new(thread.GuildId, thread.Id, starter.Id, starter.CreatedAt), cancellationToken);
        var reply = await assistance.For(request with { AllowClarification = true }, delayed ? "discord-forum-delayed" : "discord-forum", cancellationToken);
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
        var message = SafeMessages.Message(DiscordAnswers.Format(answer, options.Value));
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
            await rest.SendMessageAsync(threadId, SafeMessages.Message(options.Value.Discord.ErrorReply));
        }
        catch (Exception exception)
        {
            // The apology itself failed to post (thread deleted, missing permission); there is nothing more to
            // do but record it — the answer path still returns without throwing.
            logger.ApologyFailed(exception, threadId);
        }
    }
}
