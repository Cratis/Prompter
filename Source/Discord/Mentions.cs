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
/// Answers questions asked by mentioning the bot in any channel it can read, and answers every plain message
/// in the dedicated ask channel (<see cref="DiscordOptions.AskChannelId"/>) without requiring a mention.
/// </summary>
/// <param name="client">The gateway client, used to read the bot's own identity for self-mention detection.</param>
/// <param name="rest">The REST client used to send follow-up messages.</param>
/// <param name="assistance">Documentation answers and separate advisory hints.</param>
/// <param name="admission">The cheap shared admission guard.</param>
/// <param name="work">The global concurrency and durable spend guard.</param>
/// <param name="context">Request-only bounded conversation history.</param>
/// <param name="options">The Prompter options carrying the Discord ask-channel identifier.</param>
/// <param name="logger">Logger for diagnostics.</param>
/// <remarks>
/// The bot's own user id is read from <see cref="GatewayClient.Id"/>, which NetCord decodes from the bot token
/// itself — so it is available immediately at construction (no wait for the READY payload) and self-mention
/// detection needs no per-message or startup REST lookup.
/// </remarks>
public class Mentions(
    GatewayClient client,
    RestClient rest,
    ConversationAssistance assistance,
    Admission admission,
    PaidWork work,
    ConversationContextReader context,
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
    /// <returns>
    /// A tuple whose <c language="csharp">ShouldAnswer</c> is <see langword="true"/> when the author is human and either the bot
    /// is mentioned (via <c language="csharp">&lt;@id&gt;</c> or the nickname form <c language="csharp">&lt;@!id&gt;</c>) or the message lives in
    /// the ask channel, and the remaining text is a non-empty <c language="csharp">Question</c> with the mention stripped. Role
    /// mentions (<c language="csharp">&lt;@&amp;id&gt;</c>), <c language="csharp">@everyone</c>, and <c language="csharp">@here</c> never trigger an answer on their own.
    /// </returns>
    public static (bool ShouldAnswer, string Question) ResolveQuestion(string content, ulong botId, bool authorIsBot, ulong channelId, ulong? askChannelId)
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
        if (!mentioned && !inAskChannel)
        {
            return (false, string.Empty);
        }

        var question = content
            .Replace(nicknameMention, string.Empty, StringComparison.Ordinal)
            .Replace(userMention, string.Empty, StringComparison.Ordinal)
            .Trim();

        return question.Length == 0 ? (false, string.Empty) : (true, question);
    }

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
            var question = CurrentQuestion(message);
            if (question is null)
            {
                return;
            }

            var decision = admission.TryEnter(new(message.GuildId, message.ChannelId, message.Author.Id), question);
            if (!decision.Allowed)
            {
                await Refuse(message, decision.Reply);
                return;
            }

            await using var lease = await work.BeginInteractive();
            lease.Activate();

            await Reply(message, question, lease.Token);
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

    string? CurrentQuestion(Message message)
    {
        if (!Supported(message))
        {
            return null;
        }

        var (shouldAnswer, question) = ResolveQuestion(message.Content, client.Id, message.Author.IsBot, message.ChannelId, options.Value.Discord.AskChannelId);
        return shouldAnswer ? question : null;
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

    async Task Reply(Message message, string question, CancellationToken cancellationToken)
    {
        var source = message.ChannelId == options.Value.Discord.AskChannelId ? "discord-ask-channel" : "discord-mention";
        logger.AnsweringQuestion(source);
        var request = message.GuildId is { } guildId
            ? await context.For(question, new(guildId, message.ChannelId, message.Id, message.CreatedAt), cancellationToken)
            : new Question(ConversationText.Sanitize(question)) { Conversation = new([], IsDegraded: true) };
        var reply = await assistance.For(request, source, cancellationToken);
        var answer = reply.Answer;
        var chunks = DiscordAnswers.Split(answer);
        for (var index = 0; index < chunks.Count; index++)
        {
            await SendChunk(message, chunks[index], index == 0, index == chunks.Count - 1 ? answer.InteractionId : null);
        }
        await SendAdvice(message, reply);
    }

    async Task SendAdvice(Message message, ConversationReply reply)
    {
        var advice = DiscordAnswers.Advice(reply);
        if (advice.Length > 0)
        {
            await SendChunk(message, advice, first: false, feedback: null);
        }
    }

    async Task SendChunk(Message message, string content, bool first, long? feedback)
    {
        if (first)
        {
            var reply = new ReplyMessageProperties { Content = content };
            if (feedback is { } id)
            {
                reply.Components = [FeedbackButtonRow.For(id)];
            }

            await message.ReplyAsync(reply);
            return;
        }

        var followup = new MessageProperties { Content = content };
        if (feedback is { } followupId)
        {
            followup.Components = [FeedbackButtonRow.For(followupId)];
        }

        await rest.SendMessageAsync(message.ChannelId, followup);
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
}
