// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.GitHub;

/// <summary>
/// Files and comments on GitHub issues over the REST API.
/// </summary>
/// <param name="httpClient">The client, configured with the API base address and bearer token.</param>
/// <param name="options">The Prompter options carrying the owner and routing.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class Issues(
    HttpClient httpClient,
    IOptions<PrompterOptions> options,
    ILogger<Issues> logger) : IIssues
{
    /// <inheritdoc/>
    public async Task<FiledIssue> File(
        string repository,
        string title,
        string body,
        IReadOnlyList<string> labels,
        CancellationToken cancellationToken = default)
    {
        var owner = options.Value.GitHub.Owner;
        var response = await httpClient.PostAsJsonAsync(
            $"repos/{owner}/{repository}/issues",
            new { title, body, labels },
            cancellationToken);

        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = document.RootElement;
        var number = root.GetProperty("number").GetInt32();
        var url = root.GetProperty("html_url").GetString() ?? string.Empty;

        logger.FiledIssue(repository, number);

        return new FiledIssue(number, repository, url);
    }

    /// <inheritdoc/>
    public async Task Comment(string repository, int number, string body, CancellationToken cancellationToken = default)
    {
        var owner = options.Value.GitHub.Owner;
        var response = await httpClient.PostAsJsonAsync(
            $"repos/{owner}/{repository}/issues/{number.ToString(CultureInfo.InvariantCulture)}/comments",
            new { body },
            cancellationToken);

        response.EnsureSuccessStatusCode();

        logger.CommentedOnIssue(repository, number);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ExistingIssue>> FindSimilar(
        string repository,
        string title,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var owner = options.Value.GitHub.Owner;
            var query = Uri.EscapeDataString($"repo:{owner}/{repository} is:issue is:open {title}");
            using var document = await httpClient.GetFromJsonAsync<JsonDocument>(
                $"search/issues?q={query}&per_page=3",
                cancellationToken);

            if (document is null || !document.RootElement.TryGetProperty("items", out var items))
            {
                return [];
            }

            return [.. items.EnumerateArray().Select(item => new ExistingIssue(
                item.GetProperty("number").GetInt32(),
                item.GetProperty("title").GetString() ?? string.Empty,
                item.GetProperty("html_url").GetString() ?? string.Empty))];
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException)
        {
            // A duplicate check is a courtesy, not a gate. Search is the most rate-limited part of the API,
            // so a failure here must never stop someone reporting a problem — the report goes ahead without
            // the "this looks like #123" hint.
            logger.DuplicateSearchFailed(exception, repository);

            return [];
        }
    }
}
