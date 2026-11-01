// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Prompter;

/// <summary>
/// Controls one independently switchable Cratis site.
/// </summary>
public class SiteOptions
{
    /// <summary>Gets or sets whether this site is indexed.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets the HTTPS site root.</summary>
    public string BaseUrl { get; set; } = "https://cratis.no";

    /// <summary>Gets or sets the sitemap path, or empty to use configured pages only.</summary>
    public string Sitemap { get; set; } = string.Empty;

    /// <summary>Gets or sets whether /llms.txt is included.</summary>
    public bool LlmsTxt { get; set; }

    /// <summary>Gets or sets whether Markdown mirrors are tried first.</summary>
    public bool MarkdownMirrors { get; set; }

    /// <summary>Gets or sets the configured absolute paths.</summary>
    public IList<string> Pages { get; set; } = [];

    /// <summary>Gets or sets path prefixes excluded from the crawl.</summary>
    public IList<string> ExcludedPaths { get; set; } = [];

    /// <summary>Gets or sets the maximum number of pages.</summary>
    public int MaxPages { get; set; } = 50;

    /// <summary>Gets whether the root and crawl bounds are safe.</summary>
    public bool IsValid => Uri.TryCreate(BaseUrl, UriKind.Absolute, out var url) &&
        url.Scheme == Uri.UriSchemeHttps && url.IsDefaultPort && url.UserInfo.Length == 0 &&
        url.AbsolutePath == "/" && url.Query.Length == 0 && url.Fragment.Length == 0 &&
        !IPAddress.TryParse(url.Host, out _) && MaxPages is >= 1 and <= 500 &&
        Pages.Concat(ExcludedPaths).All(path => path.StartsWith('/'));
}
