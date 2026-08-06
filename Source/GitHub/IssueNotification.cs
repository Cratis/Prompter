// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Prompter.GitHub;

/// <summary>
/// Composes the message announcing a new issue in a maintainer channel.
/// </summary>
/// <remarks>
/// The plain "issue opened" notification is better served by GitHub's own Discord webhook, which needs no
/// software from us and keeps working when Prompter does not. This exists for the one thing that webhook
/// cannot say: whether the documentation already answers the issue. That single line is the difference
/// between a notification and triage — it tells a maintainer whether to expect a five-minute reply or a real
/// gap in the product.
/// </remarks>
public static class IssueNotification
{
    /// <summary>
    /// Builds the announcement.
    /// </summary>
    /// <param name="issue">The opened issue.</param>
    /// <param name="answered">
    /// Whether Prompter answered it from the documentation. <see langword="null"/> when answering was not
    /// attempted, because the repository has not opted in.
    /// </param>
    /// <returns>The message to post.</returns>
    public static string For(OpenedIssue issue, bool? answered)
    {
        var verdict = answered switch
        {
            true => "Prompter answered it from the docs.",
            false => "Prompter could not answer it from the docs — this may be a real gap.",
            null => string.Empty
        };

        var header = string.Create(
            CultureInfo.InvariantCulture,
            $"**New issue in {issue.Repository}** — [#{issue.Number} {issue.Title}]({issue.Url})");

        return verdict.Length > 0 ? $"{header}\n{verdict}" : header;
    }
}
