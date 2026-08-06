// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using Cratis.Prompter.GitHub;
using NetCord;
using NetCord.Rest;

namespace Cratis.Prompter.Discord;

/// <summary>
/// Renders the preview a reporter confirms before anything is filed, and the buttons under it.
/// </summary>
/// <remarks>
/// The preview is the consent step (decision D-16), so it has to show what will actually be public: the
/// title, the repository, and enough of the body to notice if the model got it wrong. It is ephemeral —
/// only the reporter sees it — so a draft they abandon leaves nothing behind in the channel either.
/// </remarks>
public static class IssuePreview
{
    /// <summary>
    /// The label on the button that files the issue.
    /// </summary>
    public const string FileLabel = "Create issue";

    /// <summary>
    /// The label on the button that discards the draft.
    /// </summary>
    public const string CancelLabel = "Cancel";

    /// <summary>
    /// The most body characters shown in the preview, keeping the whole message inside Discord's limit.
    /// </summary>
    public const int BodyPreviewLength = 1200;

    /// <summary>
    /// Builds the preview text.
    /// </summary>
    /// <param name="draft">The drafted issue.</param>
    /// <param name="repository">The repository it would be filed in.</param>
    /// <param name="routed">Whether the repository came from the draft's product or from the fallback.</param>
    /// <param name="similar">Open issues that look like duplicates, best match first.</param>
    /// <returns>The message content to show ephemerally.</returns>
    public static string Text(IssueDraft draft, string repository, bool routed, IReadOnlyList<ExistingIssue> similar)
    {
        var preview = new StringBuilder()
            .Append(CultureInfo.InvariantCulture, $"**{draft.Title}**\n")
            .Append(CultureInfo.InvariantCulture, $"`{repository}` · {draft.Kind.ToString().ToLowerInvariant()}");

        if (!routed)
        {
            // Say so rather than quietly filing in the fallback: the reporter is the only person who can
            // correct a product the conversation never named.
            preview.Append(" · *couldn't tell which product — check this*");
        }

        preview.Append("\n\n").Append(Truncate(draft.Body, BodyPreviewLength));

        if (similar.Count > 0)
        {
            preview.Append("\n\n**Already open, possibly the same thing**\n");

            foreach (var issue in similar)
            {
                preview.Append(CultureInfo.InvariantCulture, $"- [#{issue.Number} {issue.Title}]({issue.Url})\n");
            }
        }

        preview.Append("\n*Only you can see this. Nothing is filed until you say so.*");

        return preview.ToString();
    }

    /// <summary>
    /// Builds the action row carrying the confirm and cancel buttons.
    /// </summary>
    /// <param name="token">The token identifying the held draft.</param>
    /// <returns>The action row to attach to the preview.</returns>
    public static ActionRowProperties Buttons(string token) => new(
    [
        new ButtonProperties(IssueButton.CustomId(IssueButton.FileAction, token), FileLabel, ButtonStyle.Success),
        new ButtonProperties(IssueButton.CustomId(IssueButton.CancelAction, token), CancelLabel, ButtonStyle.Secondary)
    ]);

    static string Truncate(string body, int limit)
    {
        var text = body.Trim();

        return text.Length <= limit ? text : text[..limit].TrimEnd() + "…";
    }
}
