// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Discord;

/// <summary>
/// Encodes and decodes the custom id carried by a feedback button. The custom id is
/// <c>feedback:&lt;verdict&gt;:&lt;interaction-id&gt;</c> - the <see cref="Prefix"/> routes the click to the
/// feedback handler, and the verdict and interaction id let the handler write the verdict back to the right
/// row without any per-message state. The separator matches NetCord's component-interaction parameter
/// separator, so a click on <c>feedback:up:42</c> is routed by its <see cref="Prefix"/>.
/// </summary>
public static class FeedbackButton
{
    /// <summary>
    /// The custom-id prefix that routes a button click to the feedback handler.
    /// </summary>
    public const string Prefix = "feedback";

    const char Separator = ':';

    /// <summary>
    /// Builds the custom id for a feedback button.
    /// </summary>
    /// <param name="verdict">The verdict the button records when clicked.</param>
    /// <param name="interactionId">The id of the interaction the answer was recorded as.</param>
    /// <returns>The custom id, at most 100 characters as Discord requires.</returns>
    public static string CustomId(FeedbackVerdict verdict, long interactionId) =>
        string.Create(CultureInfo.InvariantCulture, $"{Prefix}{Separator}{verdict.ToText()}{Separator}{interactionId}");

    /// <summary>
    /// Decodes a feedback button's custom id.
    /// </summary>
    /// <param name="customId">The custom id from the clicked button.</param>
    /// <returns>
    /// The decoded <see cref="FeedbackClick"/>, or <see langword="null"/> when <paramref name="customId"/> is
    /// not a well-formed feedback custom id (wrong prefix, wrong shape, unknown verdict, or a non-numeric id).
    /// </returns>
    public static FeedbackClick? Parse(string customId)
    {
        var parts = customId.Split(Separator);
        if (parts.Length != 3 || parts[0] != Prefix)
        {
            return null;
        }

        if (!FeedbackVerdicts.TryParse(parts[1], out var verdict))
        {
            return null;
        }

        return long.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var interactionId)
            ? new FeedbackClick(verdict, interactionId)
            : null;
    }
}
