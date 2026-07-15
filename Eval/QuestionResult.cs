// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Prompter.Eval;

/// <summary>
/// Represents the scored outcome of running one golden question through the bot.
/// </summary>
/// <param name="Id">The golden question id.</param>
/// <param name="Product">The product the question targets.</param>
/// <param name="Type">The question type.</param>
/// <param name="ExpectedRefusal">Whether the golden set expected a refusal (out of scope).</param>
/// <param name="ActualRefusal">Whether the bot actually refused.</param>
/// <param name="RefusalCorrect">Whether the refusal decision matched the expectation.</param>
/// <param name="CitationHit">
/// Whether retrieval surfaced an expected page, for in-scope questions; <see langword="null"/> for
/// out-of-scope questions (where citation-hit does not apply).
/// </param>
/// <param name="Groundedness">
/// The judge's groundedness score (1-5) for in-scope answered questions; <see langword="null"/> when the
/// question was refused or not judged.
/// </param>
/// <param name="Confidence">The top passage score behind the answer.</param>
/// <param name="Citations">The pages the answer cited.</param>
/// <param name="Error">The failure message when the question could not be scored; otherwise <see langword="null"/>.</param>
internal sealed record QuestionResult(
    string Id,
    string Product,
    string Type,
    bool ExpectedRefusal,
    bool ActualRefusal,
    bool RefusalCorrect,
    bool? CitationHit,
    double? Groundedness,
    double Confidence,
    IReadOnlyList<string> Citations,
    string? Error)
{
    /// <summary>The groundedness score (out of 5) at or above which an answer counts as grounded.</summary>
    public const double GroundednessPassThreshold = 4.0;

    /// <summary>
    /// Gets a value indicating whether the question passed overall. Out-of-scope questions pass when refused;
    /// in-scope questions pass when answered, retrieval hit an expected page, and the answer is grounded.
    /// </summary>
    public bool Passed =>
        Error is null &&
        RefusalCorrect &&
        (ExpectedRefusal || (CitationHit == true && Groundedness is double score && score >= GroundednessPassThreshold));

    /// <summary>
    /// Renders a compact one-line summary of this result for the console.
    /// </summary>
    /// <returns>The console line.</returns>
    public string ConsoleLine()
    {
        var verdict = Passed ? "PASS" : "FAIL";

        if (Error is not null)
        {
            return $"  [{Id}] {verdict}  error: {Error}";
        }

        var citation = CitationHit switch
        {
            true => "cite=hit",
            false => "cite=miss",
            null => "cite=n/a",
        };

        var refusal = (ExpectedRefusal, ActualRefusal) switch
        {
            (true, true) => "refusal=ok",
            (true, false) => "refusal=answered",
            (false, true) => "refusal=refused",
            (false, false) => "refusal=answered",
        };

        var grounded = Groundedness is double score
            ? "grounded=" + score.ToString("0.#", CultureInfo.InvariantCulture)
            : "grounded=n/a";

        return $"  [{Id}] {verdict}  {citation} {refusal} {grounded}";
    }
}
