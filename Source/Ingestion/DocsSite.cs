// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// Represents an implementation of <see cref="IDocsSite"/> working against the published Starlight site,
/// which exposes a sitemap and a markdown mirror per page (verified against cratis.io 2026-07-15).
/// </summary>
/// <param name="httpClient">The client configured with the docs site as base address.</param>
/// <param name="options">The Prompter options carrying the ingestion configuration.</param>
/// <param name="logger">Logger for diagnostics.</param>
public partial class DocsSite(HttpClient httpClient, IOptions<PrompterOptions> options, ILogger<DocsSite> logger) : IDocsSite
{
    [GeneratedRegex("<loc>(?<url>[^<]+)</loc>", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex SitemapLocation { get; }

    /// <summary>
    /// Parses page URLs out of a sitemap, normalizing every page to its markdown-mirror URL and
    /// excluding generated content that does not ground answers well.
    /// </summary>
    /// <param name="sitemap">The XML content of the sitemap.</param>
    /// <param name="excludedPathSegments">Path segments whose pages are excluded from ingestion.</param>
    /// <returns>The page URLs to ingest.</returns>
    public static IEnumerable<PageUrl> ParsePageUrls(string sitemap, IEnumerable<string> excludedPathSegments) =>
        PageUrls(SitemapEntries(sitemap), excludedPathSegments);

    /// <inheritdoc/>
    public async IAsyncEnumerable<DocsPage> GetPages([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var limits = options.Value.Ingestion;
        var sitemap = await Fetch(new Uri(new Uri(options.Value.DocsSiteUrl.TrimEnd('/') + "/"), "sitemap-0.xml"), limits.MaxSitemapBytes, cancellationToken);

        foreach (var url in BoundedPageUrls(sitemap, limits))
        {
            if (!IsAllowed(url))
            {
                logger.FailedFetchingPage(url, new IncompleteCrawl());
                continue;
            }
            var markdown = await Fetch(new Uri(url.Value, UriKind.Absolute), limits.Sites.MaxPageBytes, cancellationToken);

            yield return new(url, markdown);
        }
    }

    static IEnumerable<string> SitemapEntries(string sitemap)
    {
        for (var match = SitemapLocation.Match(sitemap); match.Success; match = match.NextMatch()) yield return match.Groups["url"].Value;
    }

    static IEnumerable<PageUrl> PageUrls(IEnumerable<string> urls, IEnumerable<string> excluded) => urls
        .Where(url => !excluded.Any(segment => url.Contains(segment, StringComparison.OrdinalIgnoreCase)))
        .Select(ToMarkdownMirror)
        .Distinct()
        .Select(url => new PageUrl(url));

    static IEnumerable<PageUrl> BoundedPageUrls(string sitemap, IngestionOptions limits)
    {
        try
        {
            var entries = SitemapEntries(sitemap).Take(limits.MaxSitemapEntries + 1).ToArray();
            if (entries.Length > limits.MaxSitemapEntries) throw new IncompleteCrawl();
            return PageUrls(entries, limits.ExcludedPathSegments);
        }
        catch (RegexMatchTimeoutException)
        {
            throw new IncompleteCrawl();
        }
    }

    static string ToMarkdownMirror(string url)
    {
        var trimmed = url.TrimEnd('/');

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var absolute)) throw new IncompleteCrawl();

        return absolute.AbsolutePath.Length <= 1 ? $"{trimmed}/index.md" : $"{trimmed}.md";
    }

    bool IsAllowed(PageUrl url) => Uri.TryCreate(url.Value, UriKind.Absolute, out var page) && IsAllowed(page);

    bool IsAllowed(Uri page)
    {
        var host = new Uri(options.Value.DocsSiteUrl).Host;
        var twin = host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? host[4..] : $"www.{host}";
        return SourceHosts.IsFetchable(page, host) || SourceHosts.IsFetchable(page, twin);
    }

    async Task<string> Fetch(Uri url, int maxBytes, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(BoundedContent.ReadTimeout);
        try
        {
            return await ReadResponse(url, maxBytes, deadline.Token);
        }
        catch (HttpRequestException)
        {
            throw new IncompleteCrawl();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new IncompleteCrawl();
        }
    }

    async Task<string> ReadResponse(Uri url, int maxBytes, CancellationToken cancellationToken)
    {
        if (!IsAllowed(url)) throw new IncompleteCrawl();
        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if ((int)response.StatusCode is not (301 or 302 or 303 or 307 or 308)) return await BoundedContent.Read(response, maxBytes, cancellationToken);

        var destination = RedirectDestination(url, response.Headers.Location);
        logger.RedirectedToHost(destination.Host);
        using var redirected = await httpClient.GetAsync(destination, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        return await BoundedContent.Read(redirected, maxBytes, cancellationToken);
    }

    Uri RedirectDestination(Uri url, Uri? location)
    {
        if (location is null || !Uri.TryCreate(url, location, out var destination)) throw new IncompleteCrawl();
        if (!IsAllowed(destination)) throw new IncompleteCrawl();
        return destination;
    }
}
