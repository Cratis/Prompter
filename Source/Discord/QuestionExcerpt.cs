// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Prompter.Discord;

/// <summary>Produces a deterministic sanitized excerpt without a model call.</summary>
public static partial class QuestionExcerpt
{
    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant, 100)]
    private static partial Regex Whitespace { get; }

    /// <summary>Sanitizes, collapses whitespace and bounds text, including the ellipsis.</summary>
    /// <param name="text">The transient question text.</param>
    /// <param name="maxLength">Maximum excerpt length.</param>
    /// <returns>The bounded excerpt.</returns>
    public static string For(string text, int maxLength)
    {
        var clean = Whitespace.Replace(ConversationText.Sanitize(text), " ").Trim();
        if (maxLength <= 0)
        {
            return string.Empty;
        }
        return clean.Length <= maxLength ? clean : $"{clean[..(maxLength - 1)]}…";
    }
}
