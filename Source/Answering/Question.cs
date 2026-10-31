// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Answering;

/// <summary>
/// Represents a question asked by a community member.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record Question(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The value used when a question is not set.
    /// </summary>
    public static readonly Question NotSet = new(string.Empty);

    /// <summary>Gets request-only history. It is excluded from serialization and retrieval.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public ConversationContext Conversation { get; init; } = ConversationContext.Empty;

    /// <summary>Gets the request-only retrieval scope. It is excluded from serialization and never stored.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public InteractionProduct? Scope { get; init; }

    /// <summary>Gets whether the interactive caller can resolve a product clarification.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool AllowClarification { get; init; }

    public static implicit operator string(Question question) => question.Value;

    public static implicit operator Question(string value) => new(value);
}
