// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Prompter.Protection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord.Rest;

namespace Cratis.Prompter.Discord;

/// <summary>Authorizes an explicit human hand-off without retaining question content.</summary>
/// <param name="rest">Discord REST access.</param>
/// <param name="admission">Shared admission limits.</param>
/// <param name="followUps">In-memory hand-off state.</param>
/// <param name="options">Configuration.</param>
/// <param name="clock">Expiry clock.</param>
/// <param name="logger">Content-free diagnostics.</param>
public class HumanHandOffs(RestClient rest, Admission admission, ForumFollowUps followUps, IOptions<PrompterOptions> options, TimeProvider clock, ILogger<HumanHandOffs> logger)
{
    /// <summary>No role is configured.</summary>
    public const string NotConfiguredReply = "Human hand-off is not configured here.";

    /// <summary>The action is unavailable.</summary>
    public const string UnavailableReply = "This human hand-off is unavailable.";

    /// <summary>Only the asker may request a hand-off.</summary>
    public const string OnlyAskerReply = "Only the person who asked can request a human.";

    /// <summary>A hand-off was already requested.</summary>
    public const string AlreadyRequestedReply = "A human has already been asked here. Please give them time to respond.";

    /// <summary>The original question is unavailable.</summary>
    public const string QuestionGoneReply = "I couldn't find the original question.";

    /// <summary>The hand-off succeeded.</summary>
    public const string RequestedReply = "A human has been asked to help here.";

    /// <summary>The hand-off failed.</summary>
    public const string FailedReply = "I couldn't reach a human here. Please ask a moderator directly.";

    readonly Lock _lock = new();
    readonly TransientLimits _requests = new(options.Value.Protection.LocalCapacity);

    /// <summary>Requests a human after admission and ownership checks.</summary>
    /// <param name="click">The content-free click context.</param>
    /// <param name="cancellationToken">Cancels Discord access.</param>
    /// <returns>An ephemeral status message.</returns>
    public async Task<string> Request(HandOffClick click, CancellationToken cancellationToken)
    {
        if (options.Value.Discord.MaintainerRoleId is not { } role)
        {
            return NotConfiguredReply;
        }
        var action = AnswerActionButton.Parse(click.CustomId);
        if (action?.Action != AnswerActionButton.HumanPrefix || click.Actor.GuildId is null)
        {
            return UnavailableReply;
        }
        if (!admission.TryEnter(click.Actor, click.CustomId).Allowed)
        {
            return options.Value.Discord.RateLimitedReply;
        }
        var found = await Find(action.Question, click.CommandAsker, cancellationToken);
        if (found is null || found.Asker is null)
        {
            return QuestionGoneReply;
        }
        if (found.Asker != click.Actor.UserId)
        {
            return OnlyAskerReply;
        }
        if (!Reserve(click.Conversation))
        {
            return AlreadyRequestedReply;
        }
        var link = action.Question.HasMessage ? action.Question : new(click.Actor.ChannelId, click.ClickedMessageId);
        return await Post(click, role, found, link.JumpLink(click.Actor.GuildId.Value), cancellationToken);
    }

    async Task<Found?> Find(QuestionLink question, ulong? commandAsker, CancellationToken cancellationToken)
    {
        if (!question.HasMessage)
        {
            return new(commandAsker, string.Empty);
        }
        try
        {
            var original = await rest.GetMessageAsync(question.ChannelId, question.MessageId, cancellationToken: cancellationToken);
            return new(original.Author.Id, QuestionExcerpt.For(original.Content, 200));
        }
        catch (RestException exception)
        {
            logger.QuestionUnavailable((int)exception.StatusCode, exception.Error?.Code);
            return null;
        }
    }

    async Task<string> Post(HandOffClick click, ulong role, Found found, string link, CancellationToken cancellationToken)
    {
        var quote = found.Excerpt.Length == 0 ? string.Empty : $" “{found.Excerpt}”";
        try
        {
            var message = new MessageProperties
            {
                Content = $"<@&{role}> <@{found.Asker}> would like a human to help with this question:{quote} {link}",
                AllowedMentions = AllowedMentionsProperties.None.WithAllowedRoles([role])
            };
            await rest.SendMessageAsync(click.Actor.ChannelId, message, cancellationToken: cancellationToken);
            followUps.HandOff(click.Actor.ChannelId);
            return RequestedReply;
        }
        catch (RestException exception)
        {
            logger.SendUnavailable((int)exception.StatusCode, exception.Error?.Code);
            return FailedReply;
        }
    }

    bool Reserve(ulong conversation)
    {
        lock (_lock)
        {
            var now = clock.GetUtcNow();
            var key = conversation.ToString(CultureInfo.InvariantCulture);
            if (!_requests.Available(key, 1, now))
            {
                return false;
            }
            _requests.Consume(key, ForumFollowUps.Lifetime, now);
            return true;
        }
    }

    sealed record Found(ulong? Asker, string Excerpt);
}
