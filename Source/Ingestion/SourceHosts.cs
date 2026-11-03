// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// Defines exact source hosts for fetching and answering.
/// </summary>
public static class SourceHosts
{
    /// <summary>Gets the configured citation hosts, including the documentation www twin.</summary>
    /// <param name="options">The source configuration.</param>
    /// <returns>The case-insensitive host allowlist.</returns>
    public static IReadOnlySet<string> CitationHosts(PrompterOptions options)
    {
        var docs = new Uri(options.DocsSiteUrl).Host;
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            docs, docs.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? docs[4..] : $"www.{docs}",
            "github.com", new Uri(options.Ingestion.Sites.Company.BaseUrl).Host,
            new Uri(options.Ingestion.Sites.Studio.BaseUrl).Host, new Uri(options.Ingestion.Sites.Direct.BaseUrl).Host
        };
    }

    /// <summary>
    /// Checks whether a corpus page can supply evidence and citations. /llms.txt is excluded on every
    /// host: it is a terse, brand-dense link index for crawlers, not a page a user-facing answer should
    /// quote, and its density otherwise wins brand-generic questions over the real overview pages.
    /// </summary>
    /// <param name="page">The page URL.</param>
    /// <param name="options">The source configuration.</param>
    /// <returns>Whether the page belongs to an allowed source.</returns>
    public static bool IsCitable(PageUrl page, PrompterOptions options) =>
        Uri.TryCreate(page.Value, UriKind.Absolute, out var url) && IsFetchable(url, url.Host) &&
        CitationHosts(options).Contains(url.Host) &&
        !url.AbsolutePath.Equals("/llms.txt", StringComparison.OrdinalIgnoreCase) &&
        (!url.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) || url.AbsolutePath.StartsWith("/Cratis/", StringComparison.OrdinalIgnoreCase));

    /// <summary>Checks whether a request stays on its exact HTTPS host.</summary>
    /// <param name="url">The request URL.</param>
    /// <param name="host">The allowed host.</param>
    /// <returns>Whether the URL is safe to request.</returns>
    public static bool IsFetchable(Uri url, string host) => url.IsAbsoluteUri && url.Scheme == Uri.UriSchemeHttps &&
        url.UserInfo.Length == 0 && url.IsDefaultPort && !IPAddress.TryParse(url.Host, out _) &&
        url.Host.Equals(host, StringComparison.OrdinalIgnoreCase);
}
