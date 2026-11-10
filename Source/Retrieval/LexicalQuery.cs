// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Prompter.Retrieval;

/// <summary>
/// Builds the lenient full-text query, where any question term may match, so a broad question whose terms
/// never occur together in one chunk still produces lexical candidates.
/// </summary>
public static partial class LexicalQuery
{
    const int MaximumTerms = 32;

    /// <summary>
    /// Terms left out of the any-term query. "Cratis" occurs in nearly every chunk, so as an any-term match it ranks
    /// pages by brand density (llms.txt, organization READMEs) rather than by the question. The strict arm still uses it.
    /// </summary>
    static readonly HashSet<string> _ubiquitous = [with(StringComparer.OrdinalIgnoreCase), "or", "cratis"];

    [GeneratedRegex(@"[\p{L}\p{N}]+", RegexOptions.CultureInvariant, RegexLimits.TimeoutMilliseconds)]
    private static partial Regex Word { get; }

    /// <summary>
    /// Builds an any-term query text for websearch_to_tsquery, which never raises syntax errors and drops stop words.
    /// </summary>
    /// <param name="question">The question text.</param>
    /// <returns>The terms joined with "or", without terms found in nearly every chunk, or an empty string when none remain.</returns>
    public static string Lenient(string question) => string.Join(
        " or ",
        Word.Matches(question)
            .Select(match => match.Value)
            .Where(word => !_ubiquitous.Contains(word))
            .DistinctBy(word => word, StringComparer.OrdinalIgnoreCase)
            .Take(MaximumTerms));
}
