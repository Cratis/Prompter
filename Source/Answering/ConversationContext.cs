// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Answering;

/// <summary>Request-scoped, untrusted conversation text; never persisted or used for retrieval.</summary>
/// <param name="Messages">Sanitized messages in chronological order, without identities.</param>
/// <param name="IsDegraded">Whether history was unavailable and the request is current-only.</param>
public sealed record ConversationContext(IReadOnlyList<ConversationMessage> Messages, bool IsDegraded = false)
{
    /// <summary>Gets the context for a single-question request.</summary>
    public static ConversationContext Empty { get; } = new([]);
}
