// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter;

/// <summary>
/// Controls ingestion of the company and product sites.
/// </summary>
public class SitesOptions
{
    /// <summary>Gets or sets the company source.</summary>
    public SiteOptions Company { get; set; } = new() { BaseUrl = "https://cratis.no", Sitemap = "sitemap.xml", LlmsTxt = true };

    /// <summary>Gets or sets the Studio source.</summary>
    public SiteOptions Studio { get; set; } = new()
    {
        BaseUrl = "https://cratis.studio", Sitemap = "sitemap.xml", LlmsTxt = true, MarkdownMirrors = true,
        ExcludedPaths = ["/privacy-policy", "/terms-and-conditions"]
    };

    /// <summary>Gets or sets the Direct source.</summary>
    public SiteOptions Direct { get; set; } = new() { BaseUrl = "https://cratis.direct", Pages = ["/"] };

    /// <summary>Gets or sets the maximum response size in bytes.</summary>
    public int MaxPageBytes { get; set; } = 1_048_576;

    /// <summary>Gets whether every site and the shared response bound are valid.</summary>
    public bool IsValid => Company.IsValid && Studio.IsValid && Direct.IsValid && MaxPageBytes is >= 4096 and <= 4_194_304;
}
