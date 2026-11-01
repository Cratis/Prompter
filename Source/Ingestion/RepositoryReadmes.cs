// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Ingestion;

/// <summary>Reads public, non-archived Cratis repository READMEs without credentials.</summary>
/// <param name="httpClient">The anonymous, non-redirecting client.</param>
/// <param name="options">The README configuration.</param>
/// <param name="logger">The source logger.</param>
public class RepositoryReadmes(HttpClient httpClient, IOptions<PrompterOptions> options, ILogger<RepositoryReadmes> logger)
{
    static readonly string[] _excludedFlags = ["archived", "fork", "disabled"];

    /// <summary>Enumerates public repository overview pages.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>README pages with GitHub blob URLs.</returns>
    public async IAsyncEnumerable<DocsPage> GetPages([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var config = options.Value.Ingestion.Readmes;
        if (config.IncludeProfile)
        {
            var profile = await Read(".github", "main", "profile/README.md", "Cratis organization profile", config.MaxReadmeCharacters, cancellationToken);
            if (profile is not null) yield return profile;
        }
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var inspected = 0;
        for (var page = 1; inspected < config.MaxRepositories; page++)
        {
            var listing = await List(page, cancellationToken);
            foreach (var repository in listing.EnumerateArray())
            {
                if (inspected++ >= config.MaxRepositories) break;
                var name = repository.GetProperty("name").GetString() ?? string.Empty;
                var branch = repository.GetProperty("default_branch").GetString() ?? string.Empty;
                if (!Safe(name) || !Safe(branch) || !seen.Add(name) || !Public(repository) ||
                    config.ExcludedRepositories.Contains(name, StringComparer.OrdinalIgnoreCase) ||
                    (options.Value.Ingestion.Samples.Enabled && name.Equals(options.Value.Ingestion.Samples.Repository.Split('/')[^1], StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }
                var description = repository.TryGetProperty("description", out var value) ? value.GetString() ?? string.Empty : string.Empty;
                var readme = await Read(name, branch, "README.md", description, config.MaxReadmeCharacters, cancellationToken);
                if (readme is not null) yield return readme;
            }
            if (listing.GetArrayLength() < 100) break;
        }
    }

    static bool Public(JsonElement repository) => !repository.GetProperty("private").GetBoolean() &&
        (!repository.TryGetProperty("visibility", out var visibility) || visibility.GetString() == "public") &&
        _excludedFlags.All(flag => !repository.TryGetProperty(flag, out var value) || !value.GetBoolean());

    static bool Safe(string value) => value.Length is > 0 and <= 100 && value is not "." and not ".." &&
        value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');

    async Task<JsonElement> List(int page, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await Send(new($"https://api.github.com/orgs/Cratis/repos?type=public&per_page=100&page={page}"), cancellationToken);
            var json = await BoundedContent.Read(response, options.Value.Ingestion.Sites.MaxPageBytes, cancellationToken);
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array) throw new IncompleteCrawl();
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            throw new IncompleteCrawl();
        }
    }

    async Task<DocsPage?> Read(string repository, string branch, string file, string description, int maxCharacters, CancellationToken cancellationToken)
    {
        var url = new Uri($"https://raw.githubusercontent.com/Cratis/{Uri.EscapeDataString(repository)}/{Uri.EscapeDataString(branch)}/{file}");
        using var response = await Send(url, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            logger.ReadmeMissing(new(url.AbsoluteUri));
            return null;
        }
        var markdown = await BoundedContent.Read(response, options.Value.Ingestion.Sites.MaxPageBytes, cancellationToken);
        return new(new($"https://github.com/Cratis/{repository}/blob/{branch}/{file}"), ReadmeMarkdown.Clean(markdown, $"Cratis/{repository}", description, maxCharacters));
    }

    async Task<HttpResponseMessage> Send(Uri url, CancellationToken cancellationToken)
    {
        if (!SourceHosts.IsFetchable(url, "api.github.com") && !SourceHosts.IsFetchable(url, "raw.githubusercontent.com")) throw new IncompleteCrawl();
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("Cratis-Prompter");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
        {
            logger.ReadmeFetchFailed(new(url.AbsoluteUri), (int)response.StatusCode);
        }
        return response;
    }
}
