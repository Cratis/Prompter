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

    [GeneratedRegex(@"[\p{L}\p{N}]+", RegexOptions.CultureInvariant, 100)]
    private static partial Regex Word { get; }

    /// <summary>
    /// Builds an any-term query text for <c>websearch_to_tsquery</c>, which never raises syntax errors and drops stop words.
    /// </summary>
    /// <param name="question">The question text.</param>
    /// <returns>The terms joined with <c>or</c>, or an empty string when the question has no words.</returns>
    public static string Lenient(string question) => string.Join(
        " or ",
        Word.Matches(question)
            .Select(match => match.Value)
            .Where(word => !word.Equals("or", StringComparison.OrdinalIgnoreCase))
            .DistinctBy(word => word, StringComparer.OrdinalIgnoreCase)
            .Take(MaximumTerms));
}
