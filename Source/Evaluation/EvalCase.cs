// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Evaluation;

/// <summary>
/// An input case in the curated YAML set.
/// </summary>
public sealed record EvalCase
{
    /// <summary>Gets the stable case identifier.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Gets the product.</summary>
    public string Product { get; init; } = string.Empty;

    /// <summary>Gets the case type.</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>Gets the question, never included in reports.</summary>
    public string Question { get; init; } = string.Empty;

    /// <summary>Gets the expected outcome: answer or refuse.</summary>
    public string Expected { get; init; } = string.Empty;

    /// <summary>Gets the expected documentation pages.</summary>
    public IReadOnlyList<string> ExpectedPages { get; init; } = [];

    /// <summary>Gets optional case-insensitive terms required in the answer.</summary>
    public IReadOnlyList<string> MustMention { get; init; } = [];
}
