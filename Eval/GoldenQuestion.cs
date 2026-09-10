// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Eval;

/// <summary>
/// Represents a single entry in the golden Q&amp;A set (<c language="csharp">Eval/golden-questions.yaml</c>, backlog P-17) - one
/// question phrased the way a Cratis community member would ask it, and what a correct response looks like.
/// </summary>
public record GoldenQuestion
{
    /// <summary>Gets the stable, unique identifier. Results and baselines key off it; it is never renumbered.</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Gets the product the question targets (e.g. <c language="csharp">chronicle</c>, <c language="csharp">arc</c>, <c language="csharp">out-of-scope</c>).</summary>
    public string Product { get; init; } = string.Empty;

    /// <summary>Gets the question type (e.g. <c language="csharp">how-to</c>, <c language="csharp">concept</c>, <c language="csharp">refusal</c>).</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>Gets the verbatim question, as it would land in Discord.</summary>
    public string Question { get; init; } = string.Empty;

    /// <summary>Gets the expected outcome: <c language="csharp">answer</c> (in scope) or <c language="csharp">refuse</c> (out of scope).</summary>
    public string Expected { get; init; } = string.Empty;

    /// <summary>
    /// Gets the canonical documentation page URLs (without the <c language="csharp">.md</c> mirror suffix) that should ground a
    /// correct answer. Populated for in-scope questions; empty for refusals.
    /// </summary>
    public IEnumerable<string> ExpectedPages { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether this question is expected to be refused (i.e. it is out of scope).
    /// </summary>
    public bool ExpectsRefusal => string.Equals(Expected, "refuse", StringComparison.OrdinalIgnoreCase);
}
