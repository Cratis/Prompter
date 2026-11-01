// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Ingestion;

/// <summary>Reads bounded text and HTML from one configured Cratis site.</summary>
/// <param name="httpClient">The anonymous, non-redirecting client.</param>
/// <param name="logger">The source logger.</param>
public partial class CratisSites(HttpClient httpClient, ILogger<CratisSites> logger)
{
    [GeneratedRegex("<loc>(?<url>[^<]+)</loc>", RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex SitemapLocation { get; }

    /// <summary>Enumerates the bounded pages of a single site.</summary>
    /// <param name="site">The site's crawl configuration.</param>
    /// <param name="maxPageBytes">The response byte limit.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Public pages using their human-facing URLs.</returns>
    public async IAsyncEnumerable<DocsPage> GetPages(SiteOptions site, int maxPageBytes, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var root = new Uri(site.BaseUrl);
        var urls = site.Pages.Select(path => new Uri(root, path)).ToList();
        if (site.LlmsTxt) urls.Add(new(root, "/llms.txt"));
        if (site.Sitemap.Length > 0)
        {
            var sitemap = await Fetch(new(root, site.Sitemap), root.Host, maxPageBytes, false, ["application/xml", "text/xml"], cancellationToken);
            foreach (var location in Locations(sitemap!))
            {
                if (Uri.TryCreate(location, UriKind.Absolute, out var url)) urls.Add(url);
            }
        }

        var safe = new List<Uri>();
        foreach (var url in urls)
        {
            if (!SourceHosts.IsFetchable(url, root.Host) || url.Query.Length > 0 || url.Fragment.Length > 0 ||
                site.ExcludedPaths.Any(path => url.AbsolutePath.StartsWith(path, StringComparison.OrdinalIgnoreCase)))
            {
                logger.SitePageSkipped(new($"{url.Scheme}://{url.Host}{url.AbsolutePath}"));
                continue;
            }
            safe.Add(url);
        }
        foreach (var url in safe.Distinct().Take(site.MaxPages))
        {
            yield return new(new(url.AbsoluteUri), await ReadPage(url, site.MarkdownMirrors, maxPageBytes, cancellationToken));
        }
    }

    static string[] Locations(string sitemap)
    {
        try
        {
            return SitemapLocation.Matches(sitemap).Select(match => WebUtility.HtmlDecode(match.Groups["url"].Value).Trim()).ToArray();
        }
        catch (RegexMatchTimeoutException)
        {
            throw new IncompleteCrawl();
        }
    }

    async Task<string> ReadPage(Uri url, bool mirrors, int maxBytes, CancellationToken cancellationToken)
    {
        if (url.AbsolutePath == "/llms.txt") return (await Fetch(url, url.Host, maxBytes, false, ["text/plain", "text/markdown"], cancellationToken))!;
        if (mirrors)
        {
            var path = url.AbsolutePath;
            path = path.EndsWith(".html", StringComparison.OrdinalIgnoreCase) ? path[..^5] + ".md" : path.TrimEnd('/') + "/index.md";
            var markdown = await Fetch(new(url, path), url.Host, maxBytes, true, ["text/markdown"], cancellationToken);
            if (markdown is not null) return markdown;
        }
        var content = await Fetch(url, url.Host, maxBytes, false, ["text/html"], cancellationToken);
        return HtmlText.ToMarkdown(content!);
    }

    async Task<string?> Fetch(Uri url, string host, int maxBytes, bool optional, string[] mediaTypes, CancellationToken cancellationToken)
    {
        if (!SourceHosts.IsFetchable(url, host)) throw new IncompleteCrawl();
        try
        {
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (optional && response.StatusCode == HttpStatusCode.NotFound) return null;
            if (response.StatusCode != HttpStatusCode.OK || !BoundedContent.MediaType(response, mediaTypes)) throw new IncompleteCrawl();
            return await BoundedContent.Read(response, maxBytes, cancellationToken);
        }
        catch (Exception error) when (error is HttpRequestException or IncompleteCrawl)
        {
            logger.SiteFetchFailed(new(url.GetLeftPart(UriPartial.Path)), error.GetType().Name);
            throw new IncompleteCrawl();
        }
    }
}
