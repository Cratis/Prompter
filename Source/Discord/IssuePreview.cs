// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using Cratis.Prompter.GitHub;
using NetCord;
using NetCord.Rest;

namespace Cratis.Prompter.Discord;

/// <summary>
/// Renders the complete payload or refuses it; never truncates consent content.
/// </summary>
public static class IssuePreview
{
    /// <summary>
    /// The confirm button label.
    /// </summary>
    public const string FileLabel = "Create issue";

    /// <summary>
    /// The cancel button label.
    /// </summary>
    public const string CancelLabel = "Cancel";

    /// <summary>
    /// The longest title accepted for one reviewable preview.
    /// </summary>
    public const int TitleLimit = 160;

    /// <summary>
    /// The longest final body, including provenance, accepted for preview.
    /// </summary>
    public const int BodyLimit = 1200;

    /// <summary>
    /// The complete encoded preview budget, below Discord's message limit.
    /// </summary>
    public const int PreviewLimit = 1900;

    /// <summary>
    /// Builds the action row, carrying only the opaque token.
    /// </summary>
    /// <param name="token">The random consent token.</param>
    /// <returns>The two preview actions.</returns>
    public static ActionRowProperties Buttons(string token) => new(
    [
        new ButtonProperties(IssueButton.CustomId(IssueButton.FileAction, token), FileLabel, ButtonStyle.Success),
        new ButtonProperties(IssueButton.CustomId(IssueButton.CancelAction, token), CancelLabel, ButtonStyle.Secondary)
    ]);

    /// <summary>
    /// Renders all posting fields, with markdown escaped and no hidden truncation.
    /// </summary>
    /// <param name="payload">The immutable posting payload.</param>
    /// <param name="routed">Whether the product was recognized.</param>
    /// <returns>The full preview, or null if it cannot safely be reviewed.</returns>
    public static string? Text(PendingIssue payload, bool routed = true)
    {
        if (!Reviewable(payload))
        {
            return null;
        }
        var fallback = routed ? string.Empty : "I couldn't tell which product this belongs to, so it will go to the default repository.\n\n";
        var text = $"{fallback}**Public GitHub issue — {Escape(payload.Owner)}/{Escape(payload.Repository)}**\n\n" +
            $"**Title:** {Escape(payload.Title)}\n\n**Body:**\n{Escape(payload.Body)}\n\n" +
            $"**Labels:** {Escape(string.Join(", ", payload.Labels))}\n\n" +
            "Review every field above. File issue posts this exact content publicly; Cancel discards it. Consent expires in 15 minutes.";
        return text.Length <= PreviewLimit ? text : null;
    }

    /// <summary>
    /// Renders bounded, separate, non-authoritative discovery advice.
    /// </summary>
    /// <param name="similar">Possibly related issues.</param>
    /// <returns>A separate advisory message, never part of the posted payload.</returns>
    public static string Advisory(IReadOnlyList<ExistingIssue> similar) =>
        "These are possibly related, not confirmed duplicates:\n" + string.Join('\n', similar.Take(3).Select(issue =>
            $"#{issue.Number} {Escape(issue.Title.Length <= 160 ? issue.Title : issue.Title[..160])}{Link(issue)}"));

    static string Link(ExistingIssue issue) =>
        issue.Url.Length <= 200 && Uri.TryCreate(issue.Url, UriKind.Absolute, out var uri) && SafeLink(uri, issue.Number)
            ? $" — <{uri.AbsoluteUri}>"
            : string.Empty;

    static bool SafeLink(Uri uri, int number) =>
        uri.Scheme.Equals("https", StringComparison.Ordinal) && uri.Host.Equals("github.com", StringComparison.Ordinal) &&
        uri is { IsDefaultPort: true, UserInfo.Length: 0, Query.Length: 0, Fragment.Length: 0 } && SafePath(uri, number);

    static bool SafePath(Uri uri, int expectedNumber) =>
        uri.AbsolutePath.Split('/') is ["", var owner, var repository, "issues", var number] &&
        owner.All(char.IsAsciiLetterOrDigit) && repository.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.') &&
        number == expectedNumber.ToString(CultureInfo.InvariantCulture);

    static bool Reviewable(PendingIssue payload) =>
        ReviewableTitle(payload.Title) && ReviewableBody(payload.Body) && ReviewableLabels(payload) &&
        new[] { payload.Owner, payload.Repository, payload.Title, payload.Body }.Concat(payload.Labels).All(Visible);

    static bool ReviewableTitle(string title) => !string.IsNullOrWhiteSpace(title) && title.Length <= TitleLimit && !title.Contains('\n') && !title.Contains('\r');

    static bool ReviewableBody(string body) => !string.IsNullOrWhiteSpace(body) && body.Length <= BodyLimit;

    static bool ReviewableLabels(PendingIssue payload) => !payload.Labels.IsDefault && payload.Labels.Length <= 10 && payload.Labels.All(ReviewableLabel);

    static bool ReviewableLabel(string label) => label.Length is > 0 and <= 50 && !label.Contains('\n') && !label.Contains('\r') && !label.Contains(',');

    static bool Visible(string value) => value.All(character =>
        (character is '\n' or '\t' || !char.IsControl(character)) && char.GetUnicodeCategory(character) != UnicodeCategory.Format);

    static string Escape(string value)
    {
        var escaped = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if ("\\`*_{}[]()#+-.!|<>~".Contains(character))
            {
                escaped.Append('\\');
            }
            escaped.Append(character);
        }
        return escaped.ToString();
    }
}
