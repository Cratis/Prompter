// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.GitHub;

/// <summary>
/// Composes what actually gets filed: the issue body and its labels.
/// </summary>
/// <remarks>
/// Two things every filed issue carries. A link back to the Discord conversation, so a maintainer can ask a
/// follow-up question of the person who hit the problem — that link is the whole follow-up mechanism, which
/// is why no Discord username is written into the issue: the thread already knows who was there, and a
/// public tracker does not need to. And a line saying Prompter filed it on someone's behalf, because an
/// issue whose provenance is unclear wastes the reader's first minute.
/// </remarks>
public static class IssueComposition
{
    /// <summary>
    /// The heading under which the drafted description is placed.
    /// </summary>
    public const string ContextHeading = "### Reported from Discord";

    /// <summary>
    /// Builds the issue body.
    /// </summary>
    /// <param name="draft">The drafted issue.</param>
    /// <param name="conversationUrl">A link to the Discord message or thread it came from, if available.</param>
    /// <returns>The body to file.</returns>
    public static string Body(IssueDraft draft, string? conversationUrl)
    {
        var body = draft.Body.Trim();
        var provenance = string.IsNullOrWhiteSpace(conversationUrl)
            ? "Filed from Discord by Prompter, on behalf of a community member."
            : $"Filed from Discord by Prompter, on behalf of a community member — [the conversation]({conversationUrl.Trim()}).";

        return $"{body}\n\n{ContextHeading}\n\n{provenance}";
    }

    /// <summary>
    /// Builds the labels for a filed issue: the configured provenance label plus one naming the kind of work.
    /// </summary>
    /// <param name="draft">The drafted issue.</param>
    /// <param name="options">The GitHub options carrying the provenance label.</param>
    /// <returns>The labels to apply, without duplicates or empties.</returns>
    /// <remarks>
    /// Labels that do not exist in the target repository are created by GitHub on use, so this never fails
    /// because a repository has not been prepared.
    /// </remarks>
    public static IReadOnlyList<string> Labels(IssueDraft draft, GitHubOptions options) =>
        new[] { options.IssueLabel, LabelFor(draft.Kind) }
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    /// <summary>
    /// Maps a kind of work to the label naming it.
    /// </summary>
    /// <param name="kind">The kind of work.</param>
    /// <returns>The label, using the names GitHub's default label set already provides where they fit.</returns>
    public static string LabelFor(IssueKind kind) => kind switch
    {
        IssueKind.Bug => "bug",
        IssueKind.Feature => "enhancement",
        IssueKind.Idea => "enhancement",
        IssueKind.Documentation => "documentation",
        _ => "question"
    };
}
