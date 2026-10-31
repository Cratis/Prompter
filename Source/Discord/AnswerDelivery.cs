// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;

namespace Cratis.Prompter.Discord;

/// <summary>Delivers answers in opt-in public threads, with the existing reply as fallback.</summary>
/// <param name="rest">The Discord REST client.</param>
/// <param name="options">The Discord configuration.</param>
/// <param name="logger">Diagnostics without question content.</param>
public class AnswerDelivery(RestClient rest, IOptions<PrompterOptions> options, ILogger<AnswerDelivery> logger)
{
    /// <summary>Delivers an answer and returns a newly opened thread identifier, if any.</summary>
    /// <param name="message">The question message.</param>
    /// <param name="question">The current question, used transiently for a thread name.</param>
    /// <param name="reply">The generated answer and advice.</param>
    /// <param name="cancellationToken">Cancels delivery.</param>
    /// <returns>The new thread identifier, or null for a normal reply.</returns>
    public Task<ulong?> Deliver(Message message, string question, ConversationReply reply, CancellationToken cancellationToken) =>
        Deliver(message, message.GuildId, message.Channel is GuildThread, question, reply, cancellationToken);

    /// <summary>Delivers an answer publicly to a selected message, opening a thread only where configured.</summary>
    /// <param name="message">The selected message.</param>
    /// <param name="guildId">The guild containing the selected message.</param>
    /// <param name="inThread">Whether the message is already in a thread.</param>
    /// <param name="question">The question, used transiently for a thread name.</param>
    /// <param name="reply">The generated answer and advice.</param>
    /// <param name="cancellationToken">Cancels delivery.</param>
    /// <returns>The new thread identifier, or null for a normal reply.</returns>
    public async Task<ulong?> Deliver(RestMessage message, ulong? guildId, bool inThread, string question, ConversationReply reply, CancellationToken cancellationToken)
    {
        var actions = AnswerActions.For(options.Value.Discord, new(message.ChannelId, message.Id), reply);
        var thread = ShouldThread(guildId, inThread, message.ChannelId) ? await TryOpenThread(message, question, cancellationToken) : null;
        var target = new Target(message, thread, actions);
        var chunks = DiscordAnswers.Split(reply.Answer);
        for (var index = 0; index < chunks.Count; index++)
        {
            await SendChunk(target, new(chunks[index], index == 0, index == chunks.Count - 1 ? reply.Answer : null), cancellationToken);
        }
        var advice = DiscordAnswers.Advice(reply);
        if (advice.Length > 0)
        {
            await SendChunk(target, new(advice, false, null), cancellationToken);
        }
        return thread;
    }

    bool ShouldThread(ulong? guildId, bool inThread, ulong channelId) => guildId is not null && !inThread &&
        options.Value.Discord.AnswerInThreadChannelIds.Contains(channelId);

    async Task<ulong?> TryOpenThread(RestMessage message, string question, CancellationToken cancellationToken)
    {
        try
        {
            var name = QuestionExcerpt.For(question, 90);
            var properties = new GuildThreadFromMessageProperties(name.Length == 0 ? "Prompter answer" : name) { AutoArchiveDuration = ThreadArchiveDuration.OneDay };
            var thread = await rest.CreateGuildThreadAsync(message.ChannelId, message.Id, properties, cancellationToken: cancellationToken);
            return thread.Id;
        }
        catch (RestException exception)
        {
            logger.ThreadUnavailable(message.ChannelId, (int)exception.StatusCode, exception.Error?.Code);
            return null;
        }
    }

    async Task SendChunk(Target target, Chunk chunk, CancellationToken cancellationToken)
    {
        if (target.Thread is null && chunk.First)
        {
            var reply = SafeMessages.Reply(chunk.Content);
            if (chunk.Last is { } last && AnswerComponents.For(last, target.Actions) is { Count: > 0 } rows)
            {
                reply.Components = [.. rows];
            }
            await target.Message.ReplyAsync(reply, cancellationToken: cancellationToken);
            return;
        }

        var followup = SafeMessages.Message(chunk.Content);
        if (chunk.Last is { } followupLast && AnswerComponents.For(followupLast, target.Actions) is { Count: > 0 } followupRows)
        {
            followup.Components = [.. followupRows];
        }
        await rest.SendMessageAsync(target.Thread ?? target.Message.ChannelId, followup, cancellationToken: cancellationToken);
    }

    sealed record Target(RestMessage Message, ulong? Thread, AnswerActions Actions);

    sealed record Chunk(string Content, bool First, Answer? Last);
}
