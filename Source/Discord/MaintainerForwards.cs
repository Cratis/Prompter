// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Digest;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord.Rest;

namespace Cratis.Prompter.Discord;

/// <summary>Forwards an asker's consented question text to the maintainer channel, without keeping it.</summary>
/// <param name="rest">Discord REST access, used to read the question at click time.</param>
/// <param name="channel">The maintainer channel poster.</param>
/// <param name="admission">Shared admission limits.</param>
/// <param name="options">Configuration.</param>
/// <param name="clock">Expiry clock.</param>
/// <param name="logger">Content-free diagnostics.</param>
public class MaintainerForwards(RestClient rest, IMaintainerChannel channel, Admission admission, IOptions<PrompterOptions> options, TimeProvider clock, ILogger<MaintainerForwards> logger)
{
    /// <summary>No maintainer channel is configured.</summary>
    public const string NotConfiguredReply = "Forwarding to the maintainers is not configured here.";

    /// <summary>The action is unavailable.</summary>
    public const string UnavailableReply = "This action is unavailable.";

    /// <summary>Only the asker may forward the question.</summary>
    public const string OnlyAskerReply = "Only the person who asked can send this to the maintainers.";

    /// <summary>The question was already forwarded.</summary>
    public const string AlreadyForwardedReply = "This question has already been sent to the maintainers.";

    /// <summary>The original question is unavailable.</summary>
    public const string QuestionGoneReply = "I couldn't find the original question.";

    /// <summary>The forward succeeded.</summary>
    public const string ForwardedReply = "Thanks, I've sent your question to the maintainers. Prompter doesn't keep a copy.";

    /// <summary>The forward failed.</summary>
    public const string FailedReply = "I couldn't reach the maintainers. Please ask a moderator directly.";

    const int MaxExcerpt = 1500;

    readonly Lock _lock = new();
    readonly TransientLimits _forwards = new(options.Value.Protection.LocalCapacity);

    /// <summary>Forwards a question after admission and ownership checks.</summary>
    /// <param name="click">The content-free click context.</param>
    /// <param name="cancellationToken">Cancels Discord access.</param>
    /// <returns>An ephemeral status message.</returns>
    public async Task<string> Forward(ForwardClick click, CancellationToken cancellationToken)
    {
        if (options.Value.Discord.MaintainerChannelId is not { } maintainerChannel)
        {
            return NotConfiguredReply;
        }
        var action = AnswerActionButton.Parse(click.CustomId);
        if (!Matches(click.Kind, action) || click.Actor.GuildId is not { } guild)
        {
            return UnavailableReply;
        }
        if (!admission.TryEnter(click.Actor, click.CustomId).Allowed)
        {
            return options.Value.Discord.RateLimitedReply;
        }
        var question = await Find(action!.Question, cancellationToken);
        if (question is null)
        {
            return QuestionGoneReply;
        }
        if (question.Asker != click.Actor.UserId)
        {
            return OnlyAskerReply;
        }
        if (!Reserve($"{click.Kind}:{action.Question.MessageId}"))
        {
            return AlreadyForwardedReply;
        }
        var content = Content(click.Kind, action.Product, question.Text, action.Question.JumpLink(guild));
        return await Post(maintainerChannel, content, cancellationToken);
    }

    static bool Matches(ForwardKind kind, AnswerActionClick? action) => action?.Question.HasMessage == true && (kind, action.Action) switch
    {
        (ForwardKind.Documentation, AnswerActionButton.DocumentedPrefix) => true,
        (ForwardKind.FeatureRequest, AnswerActionButton.RequestPrefix) => true,
        _ => false
    };

    static string Content(ForwardKind kind, string? product, string text, string link)
    {
        var excerpt = QuestionExcerpt.For(text, MaxExcerpt);
        return kind == ForwardKind.Documentation
            ? $"📚 Documentation gap: a question Prompter could not answer, forwarded by its asker (not stored by Prompter):\n>>> {excerpt}\n{link}"
            : $"💡 Feature request ({(product is null or "" || product == RoutingProduct.Unknown.Value ? "product unknown" : product)}): a capability the asker could not find, forwarded by its asker (not stored by Prompter):\n>>> {excerpt}\n{link}";
    }

    async Task<Found?> Find(QuestionLink question, CancellationToken cancellationToken)
    {
        try
        {
            var original = await rest.GetMessageAsync(question.ChannelId, question.MessageId, cancellationToken: cancellationToken);
            return new(original.Author.Id, original.Content);
        }
        catch (RestException exception)
        {
            logger.QuestionUnavailable((int)exception.StatusCode, exception.Error?.Code);
            return null;
        }
    }

    async Task<string> Post(ulong maintainerChannel, string content, CancellationToken cancellationToken)
    {
        try
        {
            await channel.Post(maintainerChannel, content, cancellationToken);
            return ForwardedReply;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.PostFailed(exception);
            return FailedReply;
        }
    }

    bool Reserve(string key)
    {
        lock (_lock)
        {
            var now = clock.GetUtcNow();
            if (!_forwards.Available(key, 1, now))
            {
                return false;
            }
            _forwards.Consume(key, ForumFollowUps.Lifetime, now);
            return true;
        }
    }

    sealed record Found(ulong Asker, string Text);
}
