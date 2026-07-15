// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Eval.Scoring;

/// <summary>
/// Normalizes documentation page URLs so the ingested markdown-mirror form and the canonical form used in
/// the golden set compare equal.
/// </summary>
/// <remarks>
/// Ingestion stores each page as its per-page markdown mirror - a trailing <c>.md</c> suffix, and the site
/// root as <c>/index.md</c> (see <c>DocsSite.ToMarkdownMirror</c>) - while the golden set lists the canonical
/// URL without the <c>.md</c> suffix and usually with a trailing slash. Normalizing both sides to the same
/// key lets citation-hit scoring intersect them reliably.
/// </remarks>
public static class PageMatching
{
    const string MarkdownSuffix = ".md";
    const string IndexSuffix = "/index";

    /// <summary>
    /// Normalizes a documentation page URL to a canonical comparison key by stripping a trailing
    /// <c>.md</c> mirror suffix, a trailing <c>/index</c> segment (the ingested form of the site root), and
    /// any trailing slash.
    /// </summary>
    /// <param name="url">The page URL to normalize.</param>
    /// <returns>The canonical comparison key for the page.</returns>
    public static string Normalize(string url)
    {
        var value = url.Trim();

        if (value.EndsWith(MarkdownSuffix, StringComparison.OrdinalIgnoreCase))
        {
            value = value[..^MarkdownSuffix.Length];
        }

        value = value.TrimEnd('/');

        if (value.EndsWith(IndexSuffix, StringComparison.OrdinalIgnoreCase))
        {
            value = value[..^IndexSuffix.Length];
        }

        return value.TrimEnd('/');
    }
}
