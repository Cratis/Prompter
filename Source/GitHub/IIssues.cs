// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.GitHub;

/// <summary>
/// Defines the GitHub issue operations Prompter needs. Narrow on purpose — filing, commenting and looking
/// for likely duplicates is the entire surface, and the credential behind it is scoped to match.
/// </summary>
public interface IIssues
{
    /// <summary>
    /// Files an issue.
    /// </summary>
    /// <param name="repository">The repository name, without the owner.</param>
    /// <param name="title">The issue title.</param>
    /// <param name="body">The issue body.</param>
    /// <param name="labels">The labels to apply.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The filed issue.</returns>
    Task<FiledIssue> File(
        string repository,
        string title,
        string body,
        IReadOnlyList<string> labels,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a comment to an existing issue.
    /// </summary>
    /// <param name="repository">The repository name, without the owner.</param>
    /// <param name="number">The issue number.</param>
    /// <param name="body">The comment body.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task Comment(string repository, int number, string body, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches open issues for likely duplicates of a title.
    /// </summary>
    /// <param name="repository">The repository name, without the owner.</param>
    /// <param name="title">The title to look for.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The most relevant open issues, best match first, or an empty list when the search is unavailable —
    /// a duplicate check that fails must never stop someone reporting a problem.
    /// </returns>
    Task<IReadOnlyList<ExistingIssue>> FindSimilar(
        string repository,
        string title,
        CancellationToken cancellationToken = default);
}
