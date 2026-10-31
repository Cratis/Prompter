// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Protection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Rest;

namespace Cratis.Prompter.Discord;

/// <summary>
/// Answers questions asked by mentioning the bot in any channel it can read, answers every plain message
/// in the dedicated ask channel (<see cref="DiscordOptions.AskChannelId"/>) without requiring a mention, and
/// answers replies to Prompter's own messages and follow-ups from the author of a help-forum thread it answered.
/// </summary>
/// <param name="client">The gateway client, used to read the bot's own identity for self-mention detection.</param>
/// <param name="delivery">The answer delivery and optional thread creation.</param>
/// <param name="assistance">Documentation answers and separate advisory hints.</param>
/// <param name="admission">The cheap shared admission guard.</param>
/// <param name="work">The global concurrency and durable spend guard.</param>
/// <param name="context">Request-only bounded conversation history.</param>
/// <param name="followUps">The help-forum threads Prompter answered, whose author may keep asking without a mention.</param>
/// <param name="options">The Prompter options carrying the Discord ask-channel identifier.</param>
/// <param name="logger">Logger for diagnostics.</param>
/// <remarks>
/// The bot's own user id is read from <see cref="GatewayClient.Id"/>, which NetCord decodes from the bot token
/// itself — so it is available immediately at construction (no wait for the READY payload) and self-mention
/// detection needs no per-message or startup REST lookup.
/// </remarks>
public class Mentions(
    GatewayClient client,
    AnswerDelivery delivery,
    ConversationAssistance assistance,
    Admission admission,
    PaidWork work,
    ConversationContextReader context,
    ForumFollowUps followUps,
    IOptions<PrompterOptions> options,
    ILogger<Mentions> logger) : IMessageCreateGatewayHandler
{
    /// <summary>
    /// Decides whether an incoming message should be answered and, if so, extracts the question from it.
    /// </summary>
    /// <param name="content">The raw message content.</param>
    /// <param name="botId">The bot's own user id, matched against user mentions in the content.</param>
    /// <param name="authorIsBot">Whether the message was written by a bot (including Prompter itself).</param>
    /// <param name="channelId">The identifier of the channel the message was posted in.</param>
    /// <param name="askChannelId">
    /// The configured dedicated ask channel, or <see langword="null"/> when the feature is unset.
    /// </param>
    /// <param name="addressed">
    /// Whether the message is already known to be directed at the bot (a reply to it, or a follow-up in a thread
    /// it answered), so no mention is needed.
    /// </param>
    /// <returns>
    /// A tuple whose <c language="csharp">ShouldAnswer</c> is <see langword="true"/> when the author is human and either the bot
    /// is mentioned (via <c language="csharp">&lt;@id&gt;</c> or the nickname form <c language="csharp">&lt;@!id&gt;</c>) or the message lives in
    /// the ask channel, and the remaining text is a non-empty <c language="csharp">Question</c> with the mention stripped. Role
    /// mentions (<c language="csharp">&lt;@&amp;id&gt;</c>), <c language="csharp">@everyone</c>, and <c language="csharp">@here</c> never trigger an answer on their own.
    /// </returns>
    public static (bool ShouldAnswer, string Question) ResolveQuestion(string content, ulong botId, bool authorIsBot, ulong channelId, ulong? askChannelId, bool addressed = false)
    {
        if (authorIsBot)
        {
            return (false, string.Empty);
        }

        var userMention = $"<@{botId}>";
        var nicknameMention = $"<@!{botId}>";

        var mentioned =
            content.Contains(userMention, StringComparison.Ordinal) ||
            content.Contains(nicknameMention, StringComparison.Ordinal);

        var inAskChannel = askChannelId is { } ask && channelId == ask;
        var directed = mentioned || inAskChannel || addressed;
        if (!directed)
        {
            return (false, string.Empty);
        }

        var question = content
            .Replace(nicknameMention, string.Empty, StringComparison.Ordinal)
            .Replace(userMention, string.Empty, StringComparison.Ordinal)
            .Trim();

        return question.Length == 0 ? (false, string.Empty) : (true, question);
    }

    /// <summary>Whether the message is a plain reply, in the same channel, to a message written by the bot itself.</summary>
    /// <param name="message">The incoming message.</param>
    /// <param name="botId">The bot's own user id.</param>
    /// <returns>
    /// <see langword="true"/> only when Discord delivered the replied-to message and the bot wrote it; a missing
    /// replied-to message (deleted or not delivered) fails closed.
    /// </returns>
    public static bool RepliesTo(RestMessage message, ulong botId) =>
        message.Type == MessageType.Reply &&
        message.MessageReference is { Type: MessageReferenceType.Reply } reference &&
        reference.ChannelId == message.ChannelId &&
        message.ReferencedMessage is { } referenced &&
        referenced.Author.Id == botId && referenced.Author.IsBot && referenced.WebhookId is null;

    /// <inheritdoc/>
    /// <remarks>
    /// NetCord already catches and logs a throwing gateway handler, but it won't do the two things that matter
    /// to the person who asked — send an apology and log with this handler's own context — so the whole body is
    /// guarded anyway: the user is throttled before the expensive answer path, answering runs under a timeout,
    /// and any failure is logged and answered with a short apology instead of leaving them in silence.
    /// </remarks>
    public async ValueTask HandleAsync(Message arg)
    {
        var message = arg;
        try
        {
            var current = CurrentQuestion(message);
            if (current is not { } asked)
            {
                HandOffToHelpers(message);
                return;
            }

            var question = asked.Question;

            var decision = admission.TryEnter(new(message.GuildId, message.ChannelId, message.Author.Id), question);
            if (!decision.Allowed)
            {
                await Refuse(message, decision.Reply);
                return;
            }

            await using var lease = await work.BeginInteractive();
            lease.Activate();

            await Reply(message, asked, lease.Token);
        }
        catch (WorkRefused)
        {
            await Refuse(message, admission.MayReply(new(message.GuildId, message.ChannelId, message.Author.Id)));
        }
        catch (Exception exception)
        {
            logger.AnswerFailed(exception);
            await TryApologize(message);
        }
    }

    static bool IsHelper(Message message, ulong ownerId) =>
        !message.Author.IsBot && message.WebhookId is null && message.Author.Id != ownerId &&
        message.Type is MessageType.Default or MessageType.Reply;

    ForumThread? HelpForumThread(Message message) =>
        message.Channel is GuildThread thread && options.Value.Discord.HelpForumChannelId is { } forum && thread.ParentId == forum
            ? new(thread.Id, thread.OwnerId, message.Id == thread.Id)
            : null;

    AskedQuestion? CurrentQuestion(Message message)
    {
        if (!Supported(message) || HelpForumThread(message) is { IsStarter: true })
        {
            return null;
        }

        var repliesToBot = RepliesTo(message, client.Id);
        var (shouldAnswer, question) = ResolveQuestion(
            message.Content,
            client.Id,
            message.Author.IsBot,
            message.ChannelId,
            options.Value.Discord.AskChannelId,
            ParentInScope(message) && (repliesToBot || IsAuthorFollowUp(message)));
        return shouldAnswer ? new(question, SourceOf(message, repliesToBot), repliesToBot) : null;
    }

    bool ParentInScope(Message message) => message.Channel is not GuildThread parent || admission.InScope(message.GuildId, parent.ParentId);

    ForumThread? FollowUpThread(Message message) => HelpForumThread(message) ??
        (followUps.AskerOf(message.ChannelId) is { } asker ? new(message.ChannelId, asker, false) : null);

    bool IsAuthorFollowUp(Message message) =>
        FollowUpThread(message) is { } forum && message.Author.Id == forum.OwnerId && followUps.Accepts(forum.Id);

    string SourceOf(Message message, bool repliesToBot)
    {
        if (message.ChannelId == options.Value.Discord.AskChannelId)
        {
            return "discord-ask-channel";
        }

        if (MentionsBot(message.Content))
        {
            return "discord-mention";
        }

        if (repliesToBot)
        {
            return "discord-reply";
        }

        return HelpForumThread(message) is not null ? "discord-forum-follow-up" : "discord-thread-follow-up";
    }

    bool MentionsBot(string content) =>
        content.Contains($"<@{client.Id}>", StringComparison.Ordinal) || content.Contains($"<@!{client.Id}>", StringComparison.Ordinal);

    void HandOffToHelpers(Message message)
    {
        if (FollowUpThread(message) is { IsStarter: false } forum && IsHelper(message, forum.OwnerId))
        {
            followUps.HandOff(forum.Id);
        }
    }

    bool Supported(Message message) =>
        admission.InScope(message.GuildId, message.ChannelId) && message.WebhookId is null &&
        message.Type is MessageType.Default or MessageType.Reply &&
        message.Content.Length <= Math.Min(options.Value.Protection.MaxQuestionCharacters, ConversationContextReader.MaximumCurrentCharacters);

    async Task Refuse(Message message, bool reply)
    {
        logger.RateLimited();
        if (reply)
        {
            await message.ReplyAsync(options.Value.Discord.RateLimitedReply);
        }
    }

    async Task<Question> BuildRequest(Message message, AskedQuestion asked, CancellationToken cancellationToken)
    {
        if (message.GuildId is not { } guildId)
        {
            return new Question(ConversationText.Sanitize(asked.Question)) { Conversation = new([], IsDegraded: true) };
        }

        var location = new ConversationLocation(guildId, message.ChannelId, message.Id, message.CreatedAt);
        if (asked.RepliesToBot && message.ReferencedMessage is { } referenced)
        {
            var anchor = new HistoryMessage(
                referenced.Id,
                referenced.ChannelId,
                referenced.CreatedAt,
                referenced.Content,
                referenced.Author.IsBot,
                referenced.Author.IsBot && referenced.Author.Id == client.Id,
                referenced.Type is not MessageType.Default and not MessageType.Reply);
            return await context.For(asked.Question, location, anchor, cancellationToken);
        }

        return await context.For(asked.Question, location, cancellationToken);
    }

    async Task Reply(Message message, AskedQuestion asked, CancellationToken cancellationToken)
    {
        var source = asked.Source;
        logger.AnsweringQuestion(source);
        var request = await BuildRequest(message, asked, cancellationToken);
        var reply = await assistance.For(request, source, cancellationToken);
        var thread = await delivery.Deliver(message, asked.Question, reply, cancellationToken);
        if (thread is { } opened)
        {
            followUps.Opened(opened, message.Author.Id);
        }
        else if (FollowUpThread(message) is { } forum && message.Author.Id == forum.OwnerId)
        {
            followUps.Answered(forum.Id);
        }
    }

    async Task TryApologize(Message message)
    {
        try
        {
            await message.ReplyAsync(options.Value.Discord.ErrorReply);
        }
        catch (Exception exception)
        {
            // The apology itself failed to send (channel gone, missing permission); there is nothing more to
            // do but record it — the handler still returns without throwing.
            logger.ApologyFailed(exception);
        }
    }

    sealed record AskedQuestion(string Question, string Source, bool RepliesToBot);

    sealed record ForumThread(ulong Id, ulong OwnerId, bool IsStarter);
}
