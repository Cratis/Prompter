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

    public static implicit operator string(Question question) => question.Value;

    public static implicit operator Question(string value) => new(value);
}
