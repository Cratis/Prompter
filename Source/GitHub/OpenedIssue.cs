// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.GitHub;

/// <summary>
/// A newly-opened issue, as read from a webhook delivery.
/// </summary>
/// <param name="Repository">The full name of the repository, as <c>owner/name</c>.</param>
/// <param name="Number">The issue number.</param>
/// <param name="Title">The issue title.</param>
/// <param name="Body">The issue body, which may be empty.</param>
/// <param name="Url">The issue's web address.</param>
/// <param name="OpenedByBot">Whether the issue was opened by a bot account.</param>
/// <param name="Labels">The labels the issue carries at the moment it was opened.</param>
public record OpenedIssue(
    string Repository,
    int Number,
    string Title,
    string Body,
    string Url,
    bool OpenedByBot,
    IReadOnlyList<string> Labels)
{
    /// <summary>
    /// Gets the repository name without its owner.
    /// </summary>
    public string RepositoryName => Repository.Contains('/', StringComparison.Ordinal)
        ? Repository[(Repository.IndexOf('/', StringComparison.Ordinal) + 1)..]
        : Repository;

    /// <summary>
    /// Gets the issue as a single question to answer: the title carries the intent, the body the detail.
    /// </summary>
    public string AsQuestion => string.IsNullOrWhiteSpace(Body) ? Title : $"{Title}\n\n{Body}";
}
