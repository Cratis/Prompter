// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Discord;

/// <summary>
/// Encodes and decodes the custom id carried by the buttons under an issue preview. The custom id is
/// <c>issue:&lt;action&gt;:&lt;token&gt;</c> — the <see cref="Prefix"/> routes the click to the confirmation
/// handler, and the token resolves back to the held draft, which is what keeps the issue body out of a custom
/// id that Discord caps at 100 characters.
/// </summary>
public static class IssueButton
{
    /// <summary>
    /// The custom-id prefix that routes a button click to the issue confirmation handler.
    /// </summary>
    public const string Prefix = "issue";

    /// <summary>
    /// The action that files the drafted issue.
    /// </summary>
    public const string FileAction = "file";

    /// <summary>
    /// The action that discards the drafted issue.
    /// </summary>
    public const string CancelAction = "cancel";

    const char Separator = ':';

    /// <summary>
    /// Builds the custom id for a preview button.
    /// </summary>
    /// <param name="action">The action the button performs.</param>
    /// <param name="token">The token identifying the held draft.</param>
    /// <returns>The custom id.</returns>
    public static string CustomId(string action, string token) => $"{Prefix}{Separator}{action}{Separator}{token}";

    /// <summary>
    /// Decodes a preview button's custom id.
    /// </summary>
    /// <param name="customId">The custom id from the clicked button.</param>
    /// <returns>
    /// The decoded click, or <see langword="null"/> when the custom id is not a well-formed issue custom id
    /// (wrong prefix, wrong shape, or an unknown action).
    /// </returns>
    public static IssueButtonClick? Parse(string customId)
    {
        var parts = customId.Split(Separator);
        if (parts.Length != 3 || parts[0] != Prefix)
        {
            return null;
        }

        var action = parts[1];
        if (action != FileAction && action != CancelAction)
        {
            return null;
        }

        return parts[2].Length == 0 ? null : new IssueButtonClick(action, parts[2]);
    }
}
