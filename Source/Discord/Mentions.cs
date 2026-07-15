// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Prompter.Answering;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
/// <param name="answers">The answers Prompter can give.</param>
/// <param name="interactionLog">The interaction log, used to record which message the answer landed on.</param>
/// <param name="options">The Prompter options carrying the Discord ask-channel identifier.</param>
/// <param name="logger">Logger for diagnostics.</param>
/// <remarks>
/// The bot's own user id is read from <see cref="GatewayClient.Id"/>, which Discord populates from the READY
/// payload before any message-create event is dispatched, so self-mention detection needs no per-message or
/// startup REST lookup.
/// </remarks>
public class Mentions(
    GatewayClient client,
    RestClient rest,
    IAnswers answers,
    IInteractionLog interactionLog,
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
    /// A tuple whose <c>ShouldAnswer</c> is <see langword="true"/> when the author is human and either the bot
    /// is mentioned (via <c>&lt;@id&gt;</c> or the nickname form <c>&lt;@!id&gt;</c>) or the message lives in
    /// the ask channel, and the remaining text is a non-empty <c>Question</c> with the mention stripped. Role
    /// mentions (<c>&lt;@&amp;id&gt;</c>), <c>@everyone</c>, and <c>@here</c> never trigger an answer on their own.
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
    public async ValueTask HandleAsync(Message arg)
    {
        var message = arg;
        var askChannelId = options.Value.Discord.AskChannelId;
        var (shouldAnswer, question) = ResolveQuestion(message.Content, client.Id, message.Author.IsBot, message.ChannelId, askChannelId);
        if (!shouldAnswer)
        {
            return;
        }

        var inAskChannel = askChannelId is { } ask && message.ChannelId == ask;
        var source = inAskChannel ? "discord-ask-channel" : "discord-mention";
        logger.AnsweringQuestion(source, message.Author.Id);

        var answer = await answers.For(new(question), UserHash.For(message.Author.Id), source);

        // Anchor the answer to the asking message with a reply reference on the first chunk; any further
        // chunks follow as ordinary messages in the same channel. The 👍/👎 feedback buttons ride on the
        // last chunk, and its message id is recorded against the interaction for auditing.
        var chunks = DiscordAnswers.Split(answer);
        var interactionId = answer.InteractionId;
        for (var index = 0; index < chunks.Count; index++)
        {
            var isLast = index == chunks.Count - 1;
            var feedback = isLast ? interactionId : null;

            RestMessage sent;
            if (index == 0)
            {
                var reply = new ReplyMessageProperties { Content = chunks[index] };
                if (feedback is { } replyFeedback)
                {
                    reply.Components = [FeedbackButtonRow.For(replyFeedback)];
                }

                sent = await message.ReplyAsync(reply);
            }
            else
            {
                var followup = new MessageProperties { Content = chunks[index] };
                if (feedback is { } followupFeedback)
                {
                    followup.Components = [FeedbackButtonRow.For(followupFeedback)];
                }

                sent = await rest.SendMessageAsync(message.ChannelId, followup);
            }

            if (isLast && interactionId is { } recordedId)
            {
                await interactionLog.SetAnswerMessage(recordedId, sent.Id.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}
