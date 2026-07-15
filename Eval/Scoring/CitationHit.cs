// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Eval.Scoring;

/// <summary>
/// Scores whether an in-scope answer surfaced the right documentation page - the deterministic retrieval
/// half of the golden-set score.
/// </summary>
public static class CitationHit
{
    /// <summary>
    /// Determines whether retrieval surfaced at least one of the expected grounding pages. Both sides are
    /// normalized with <see cref="PageMatching.Normalize"/> so the ingested <c>.md</c> mirror form and the
    /// canonical golden-set form intersect.
    /// </summary>
    /// <param name="expectedPages">The candidate grounding pages from the golden set (canonical form).</param>
    /// <param name="candidatePages">
    /// The pages the answer surfaced - its citations together with the retrieved passages' pages.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when a normalized candidate page matches a normalized expected page;
    /// otherwise <see langword="false"/> (including when no expected pages are supplied).
    /// </returns>
    public static bool IsHit(IEnumerable<string> expectedPages, IEnumerable<string> candidatePages)
    {
        var expected = expectedPages.Select(PageMatching.Normalize).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return expected.Count > 0 && candidatePages.Select(PageMatching.Normalize).Any(expected.Contains);
    }
}
