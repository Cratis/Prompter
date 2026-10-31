// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Answering;

/// <summary>Represents one generation result: the answer text plus the same-call triage signals.</summary>
/// <param name="Text">The answer text; never a raw structured envelope.</param>
/// <param name="Category">The triage category, <see cref="InteractionCategory.Unknown"/> when not recoverable.</param>
/// <param name="Product">The product, <see cref="InteractionProduct.OtherOrUnknown"/> when not recoverable.</param>
public record GeneratedAnswer(string Text, InteractionCategory Category, InteractionProduct Product)
{
    /// <summary>Gets the follow-up questions the model proposed, unfiltered; empty when none were recoverable.</summary>
    public IReadOnlyList<string> Suggestions { get; init; } = [];
}
