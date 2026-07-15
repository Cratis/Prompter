// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;
using NetCord;
using NetCord.Rest;

namespace Cratis.Prompter.Discord;

/// <summary>
/// Builds the thumbs-up/down feedback buttons attached to an answer. Buttons are used rather than reactions
/// so a click carries the user and the interaction directly, costs no pre-add API calls, and never fails
/// silently on an inaccessible emoji (see the M3 Discord best-practices research).
/// </summary>
public static class FeedbackButtonRow
{
    /// <summary>
    /// The label on the positive-feedback button. The emoji rides in the label text, so no custom emoji is
    /// referenced and the button cannot fail to render.
    /// </summary>
    public const string HelpfulLabel = "👍 Helpful";

    /// <summary>
    /// The label on the negative-feedback button.
    /// </summary>
    public const string NotHelpfulLabel = "👎 Not helpful";

    /// <summary>
    /// Builds the action row carrying the feedback buttons for an interaction.
    /// </summary>
    /// <param name="interactionId">The id of the interaction the answer was recorded as.</param>
    /// <returns>The action row to attach to the answer message's components.</returns>
    public static ActionRowProperties For(long interactionId) => new(
    [
        new ButtonProperties(FeedbackButton.CustomId(FeedbackVerdict.Up, interactionId), HelpfulLabel, ButtonStyle.Success),
        new ButtonProperties(FeedbackButton.CustomId(FeedbackVerdict.Down, interactionId), NotHelpfulLabel, ButtonStyle.Secondary)
    ]);
}
