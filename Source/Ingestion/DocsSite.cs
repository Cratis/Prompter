// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// Represents an implementation of <see cref="IDocsSite"/> working against the published Starlight site,
/// which exposes a sitemap and a markdown mirror per page (verified against cratis.io 2026-07-15).
/// </summary>
/// <param name="httpClient">The client configured with the docs site as base address.</param>
/// <param name="logger">Logger for diagnostics.</param>
public partial class DocsSite(HttpClient httpClient, ILogger<DocsSite> logger) : IDocsSite
{
    static readonly string[] _excludedPathSegments = ["client-snippets", "api-reference"];

    [GeneratedRegex("<loc>(?<url>[^<]+)</loc>", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex SitemapLocation { get; }

    /// <summary>
    /// Parses page URLs out of a sitemap, normalizing every page to its markdown-mirror URL and
    /// excluding generated content that does not ground answers well.
    /// </summary>
    /// <param name="sitemap">The XML content of the sitemap.</param>
    /// <returns>The page URLs to ingest.</returns>
    public static IEnumerable<PageUrl> ParsePageUrls(string sitemap) =>
        SitemapLocation
            .Matches(sitemap)
            .Select(match => match.Groups["url"].Value)
            .Where(url => !_excludedPathSegments.Any(segment => url.Contains(segment, StringComparison.OrdinalIgnoreCase)))
            .Select(ToMarkdownMirror)
            .Distinct()
            .Select(url => new PageUrl(url));

    /// <inheritdoc/>
    public async IAsyncEnumerable<DocsPage> GetPages([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var sitemap = await httpClient.GetStringAsync(new Uri("sitemap-0.xml", UriKind.Relative), cancellationToken);

        foreach (var url in ParsePageUrls(sitemap))
        {
            string markdown;
            try
            {
                markdown = await httpClient.GetStringAsync(new Uri(url.Value, UriKind.RelativeOrAbsolute), cancellationToken);
            }
            catch (HttpRequestException exception)
            {
                logger.FailedFetchingPage(url, exception);
                continue;
            }

            yield return new(url, markdown);
        }
    }

    static string ToMarkdownMirror(string url)
    {
        var trimmed = url.TrimEnd('/');

        return new Uri(trimmed, UriKind.RelativeOrAbsolute) is { IsAbsoluteUri: true } absolute && absolute.AbsolutePath.Length <= 1
            ? $"{trimmed}/index.md"
            : $"{trimmed}.md";
    }
}
