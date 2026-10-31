// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Prompter.Discord;

/// <summary>Builds and reads follow-up suggestion button custom ids. The id carries only an index, never user text.</summary>
public static class SuggestionButton
{
    /// <summary>The first segment of every suggestion custom id, and the component route.</summary>
    public const string Prefix = "suggest";

    /// <summary>The highest number of suggestion buttons on a message.</summary>
    public const int MaximumCount = 3;

    /// <summary>Builds a custom id.</summary>
    /// <param name="index">The suggestion index, 0-2.</param>
    /// <returns>The custom id.</returns>
    public static string CustomId(int index) => $"{Prefix}:{index.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>Reads a custom id.</summary>
    /// <param name="customId">The clicked custom id.</param>
    /// <returns>The index 0-2, or <see langword="null"/> for any other shape.</returns>
    public static int? Parse(string customId)
    {
        var parts = customId.Split(':');
        return parts.Length == 2 && parts[0] == Prefix && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var index) && index < MaximumCount
            ? index
            : null;
    }

    /// <summary>Finds the label of the suggestion button with the given index.</summary>
    /// <param name="buttons">The buttons on the clicked message, as custom id and label.</param>
    /// <param name="index">The clicked index.</param>
    /// <returns>The label, or <see langword="null"/> when no suggestion button has that index.</returns>
    public static string? LabelAt(IEnumerable<(string CustomId, string Label)> buttons, int index) =>
        buttons.Where(button => button.CustomId == CustomId(index) && !string.IsNullOrWhiteSpace(button.Label))
            .Select(button => button.Label).FirstOrDefault();
}
