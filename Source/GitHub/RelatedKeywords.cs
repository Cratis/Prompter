// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.GitHub;

/// <summary>Plain first-line keywords only, without user qualifiers or potential token-shaped strings.</summary>
internal static class RelatedKeywords
{
    /// <summary>Extracts at most eight plain words (at most 199 characters including spaces).</summary>
    /// <param name="text">Untrusted current request only.</param>
    /// <returns>Non-operator keywords, never user-supplied GitHub qualifiers.</returns>
    public static string From(string text)
    {
        var firstLine = text.Split('\n', '\r')[0];
        var bounded = firstLine[..Math.Min(firstLine.Length, 2000)];
        var words = ConversationText.Sanitize(bounded).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Where(IsKeyword).Distinct(StringComparer.OrdinalIgnoreCase).Take(8);
        return string.Join(' ', words);
    }

    static bool IsKeyword(string word) => word.Length is >= 2 and <= 24 && word.All(char.IsAsciiLetter) &&
        !word.Equals("AND", StringComparison.OrdinalIgnoreCase) &&
        !word.Equals("OR", StringComparison.OrdinalIgnoreCase) &&
        !word.Equals("NOT", StringComparison.OrdinalIgnoreCase);
}
