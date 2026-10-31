// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Ingestion;

/// <summary>Reads bounded published release notes from public Cratis repositories.</summary>
/// <param name="httpClient">The unauthenticated HTTP client.</param>
/// <param name="options">The ingestion configuration.</param>
/// <param name="logger">The diagnostic logger.</param>
public class ReleaseNotes(HttpClient httpClient, IOptions<PrompterOptions> options, ILogger<ReleaseNotes> logger)
{
    static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    /// <summary>Renders explicit version and publication metadata alongside a release body.</summary>
    /// <param name="product">The product repository.</param>
    /// <param name="note">The published release.</param>
    /// <returns>The source Markdown.</returns>
    public static string Markdown(string product, ReleaseNote note)
    {
        var version = note.TagName.TrimStart('v');
        var date = note.PublishedAt?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
        var display = product.Equals("cli", StringComparison.OrdinalIgnoreCase) ? "CLI" : product;
        return $"---\nproductVersion: {version}\nlastUpdated: {date}\n---\n# {display} {note.TagName}\n\n{note.Body}";
    }

    /// <summary>Enumerates supported, published release notes.</summary>
    /// <param name="cancellationToken">Cancels the crawl.</param>
    /// <returns>The complete release-note pages.</returns>
    /// <exception cref="IncompleteCrawl">GitHub did not return a complete valid response.</exception>
    public async IAsyncEnumerable<DocsPage> GetPages([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var settings = options.Value.Ingestion.ReleaseNotes;
        foreach (var repository in settings.Repositories)
        {
            var releases = await Read(repository, settings.MaxReleasesPerRepository, cancellationToken);
            var prefix = $"https://github.com/Cratis/{repository}/releases/tag/";
            var supported = releases.Take(settings.MaxReleasesPerRepository).Where(note =>
                Supported(note, settings.MaxBodyCharacters) && note.HtmlUrl?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true).ToArray();
            if (supported.Length == 0) throw new IncompleteCrawl();
            foreach (var note in supported)
            {
                yield return new(new PageUrl(note.HtmlUrl), Markdown(repository, note));
            }
        }
    }

    static bool Supported(ReleaseNote note, int maximum) =>
        !note.Draft && !note.Prerelease && HasBody(note, maximum);

    static bool HasBody(ReleaseNote note, int maximum) =>
        !string.IsNullOrWhiteSpace(note.Body) && note.Body.Length <= maximum;

    async Task<ReleaseNote[]> Read(string repository, int maximum, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/Cratis/{Uri.EscapeDataString(repository)}/releases?per_page={maximum}");
        request.Headers.UserAgent.ParseAdd("Cratis-Prompter");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.FailedReadingReleases((int)response.StatusCode);
                throw new IncompleteCrawl();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<ReleaseNote[]>(json, _jsonOptions) ?? throw new IncompleteCrawl();
        }
        catch (Exception error) when (error is HttpRequestException or JsonException || (error is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            logger.FailedReadingReleases(null);
            throw new IncompleteCrawl();
        }
    }
}
