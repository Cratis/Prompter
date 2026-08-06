// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Prompter.GitHub;

/// <summary>
/// Reads webhook deliveries and decides what Prompter does about them.
/// </summary>
public static class IssueEvents
{
    /// <summary>
    /// The delivery action this cares about.
    /// </summary>
    public const string OpenedAction = "opened";

    /// <summary>
    /// Parses a newly-opened issue out of a delivery payload.
    /// </summary>
    /// <param name="payload">The raw delivery body.</param>
    /// <returns>
    /// The opened issue, or <see langword="null"/> when the payload is not a well-formed <c>issues.opened</c>
    /// delivery. Everything else GitHub sends — edits, comments, labels, pull requests — is ignored silently,
    /// which is what lets a repository point its whole webhook here rather than a narrow event selection.
    /// </returns>
    public static OpenedIssue? ParseOpened(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object ||
                !root.TryGetProperty("action", out var action) ||
                action.GetString() != OpenedAction ||
                !root.TryGetProperty("issue", out var issue) ||
                !root.TryGetProperty("repository", out var repository))
            {
                return null;
            }

            // A pull request arrives on the issues event too, carrying a pull_request member. Answering one
            // would put a documentation reply on a code review.
            if (issue.TryGetProperty("pull_request", out _))
            {
                return null;
            }

            var labels = issue.TryGetProperty("labels", out var labelArray) && labelArray.ValueKind == JsonValueKind.Array
                ? labelArray.EnumerateArray()
                    .Select(label => label.TryGetProperty("name", out var name) ? name.GetString() : null)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Select(name => name!)
                    .ToArray()
                : [];

            var openedByBot = issue.TryGetProperty("user", out var user) &&
                user.TryGetProperty("type", out var userType) &&
                string.Equals(userType.GetString(), "Bot", StringComparison.OrdinalIgnoreCase);

            return new OpenedIssue(
                repository.TryGetProperty("full_name", out var fullName) ? fullName.GetString() ?? string.Empty : string.Empty,
                issue.TryGetProperty("number", out var number) ? number.GetInt32() : 0,
                issue.TryGetProperty("title", out var title) ? title.GetString() ?? string.Empty : string.Empty,
                issue.TryGetProperty("body", out var body) ? body.GetString() ?? string.Empty : string.Empty,
                issue.TryGetProperty("html_url", out var url) ? url.GetString() ?? string.Empty : string.Empty,
                openedByBot,
                labels);
        }
        catch (JsonException)
        {
            // Malformed payloads are not worth a failed response: the signature already proved the sender,
            // so this is GitHub sending something unexpected, which is theirs to change, not ours to reject.
            return null;
        }
    }

    /// <summary>
    /// Determines whether Prompter should answer an issue.
    /// </summary>
    /// <param name="issue">The opened issue.</param>
    /// <param name="options">The GitHub options carrying the allowlist and the opt-out label.</param>
    /// <returns><see langword="true"/> when the issue may be answered; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    /// Answering is opt-in per repository, skips bots (so two assistants cannot talk to each other), and
    /// honors the opt-out label. Whether the answer is actually posted is decided later, by whether Prompter
    /// can ground it: a refusal is never posted, because silence on a tracker costs nothing and a hedging
    /// comment costs a maintainer's attention.
    /// </remarks>
    public static bool ShouldAnswer(OpenedIssue issue, GitHubOptions options) =>
        !issue.OpenedByBot &&
        options.AnsweringRepositories.Any(allowed => string.Equals(allowed, issue.Repository, StringComparison.OrdinalIgnoreCase)) &&
        !issue.Labels.Any(label => string.Equals(label, options.OptOutLabel, StringComparison.OrdinalIgnoreCase));
}
