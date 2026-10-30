// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Routing;

/// <summary>Derives finite product choices exclusively from configured routing keys.</summary>
/// <param name="configuredProducts">Routing-map keys.</param>
internal sealed class RoutingProducts(IReadOnlyList<string> configuredProducts)
{
    /// <summary>Gets at most 31 configured keys and unknown.</summary>
    public IReadOnlyList<string> Choices { get; } = [.. configuredProducts
        .Where(IsValid).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).Take(31), "unknown"];

    /// <summary>Matches a known product only if unambiguous, without inferring filing intent.</summary>
    /// <param name="current">Bounded current request.</param>
    /// <param name="reason">Closed-enum fallback reason.</param>
    /// <returns>A safe question/clarify classification.</returns>
    public Classification Baseline(string current, ClassificationFallback reason)
    {
        var matches = Choices.Where(product => product != "unknown" && ContainsWord(current, product)).Take(2).ToArray();
        return new(
            matches.Length == 1 ? new(matches[0]) : RoutingProduct.Unknown,
            ClassificationIntent.Question,
            ClassificationAction.Clarify,
            ConfidenceBucket.Unknown,
            ClassificationSource.Deterministic,
            reason);
    }

    static bool IsValid(string product) => product.Length is > 0 and <= 40 && product != "unknown"
        && product.All(character => character is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-');

    static bool ContainsWord(string text, string word)
    {
        var start = 0;
        while ((start = text.IndexOf(word, start, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            var end = start + word.Length;
            if (IsBoundary(text, start - 1) && IsBoundary(text, end)) return true;
            start = end;
        }
        return false;
    }

    static bool IsBoundary(string text, int index) => index < 0 || index == text.Length || !IsWord(text[index]);

    static bool IsWord(char character) => char.IsLetterOrDigit(character) || character is '_' or '-';
}
