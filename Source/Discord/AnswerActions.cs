// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Routing;

namespace Cratis.Prompter.Discord;

/// <summary>Optional explicit actions attached to an answer.</summary>
/// <param name="Question">The question location.</param>
/// <param name="OfferHuman">Whether to offer a human hand-off.</param>
/// <param name="OfferDocumented">Whether to offer documentation forwarding.</param>
/// <param name="RequestProduct">The product for a feature request, or null.</param>
public sealed record AnswerActions(QuestionLink? Question, bool OfferHuman, bool OfferDocumented, string? RequestProduct)
{
    /// <summary>Gets an empty action selection.</summary>
    public static readonly AnswerActions None = new(null, false, false, null);

    /// <summary>Gets whether to offer a feature request.</summary>
    public bool OfferRequest => RequestProduct is not null;

    /// <summary>Selects actions from primitive reply facts.</summary>
    /// <param name="question">The question location.</param>
    /// <param name="isRefusal">Whether the answer is a refusal or a documentation gap.</param>
    /// <param name="humanEnabled">Whether a role is configured.</param>
    /// <param name="forwardsEnabled">Whether maintainer forwarding is configured.</param>
    /// <param name="missingCapability">Whether the capability seems missing.</param>
    /// <param name="product">The routing product.</param>
    /// <returns>The optional actions.</returns>
    public static AnswerActions For(QuestionLink question, bool isRefusal, bool humanEnabled, bool forwardsEnabled, bool missingCapability, string product) =>
        new(
            question,
            humanEnabled,
            forwardsEnabled && isRefusal && !missingCapability && question.HasMessage,
            forwardsEnabled && missingCapability && question.HasMessage ? product : null);

    /// <summary>Selects actions from the current configuration and reply.</summary>
    /// <param name="options">The Discord configuration.</param>
    /// <param name="question">The question location.</param>
    /// <param name="reply">The conversation reply.</param>
    /// <returns>The optional actions.</returns>
    public static AnswerActions For(DiscordOptions options, QuestionLink question, ConversationReply reply)
    {
        var forwards = options.MaintainerChannelId is not null;
        var missing = reply.Answer.Category == InteractionCategory.MissingCapability;
        var documented = reply.Answer.IsRefusal || reply.Answer.Category == InteractionCategory.DocsGap;
        return For(question, documented, options.MaintainerRoleId is not null, forwards, missing, ProductKey(reply.Answer.Product));
    }

    static string ProductKey(InteractionProduct product) => product switch
    {
        InteractionProduct.Chronicle => "chronicle",
        InteractionProduct.Arc => "arc",
        InteractionProduct.Fundamentals => "fundamentals",
        InteractionProduct.Components => "components",
        InteractionProduct.Cli => "cli",
        _ => RoutingProduct.Unknown.Value
    };
}
