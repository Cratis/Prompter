// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter;

/// <summary>
/// Options controlling how the published documentation site is ingested into the retrieval corpus.
/// </summary>
public class IngestionOptions
{
    /// <summary>Gets or sets the public release-note source options.</summary>
    public ReleaseNotesOptions ReleaseNotes { get; set; } = new();

    /// <summary>Gets or sets the curated sample source options.</summary>
    public SamplesOptions Samples { get; set; } = new();

    /// <summary>
    /// Gets or sets the path segments whose pages are excluded from ingestion. Any sitemap URL containing
    /// one of these segments is skipped. Defaults exclude Chronicle's generated client snippets and the
    /// generated API reference, neither of which grounds answers well (see D-9).
    /// </summary>
    public IList<string> ExcludedPathSegments { get; set; } = ["client-snippets", "api-reference"];

    /// <summary>Gets or sets the maximum sitemap response size in bytes.</summary>
    public int MaxSitemapBytes { get; set; } = 8_388_608;

    /// <summary>Gets or sets the maximum number of raw sitemap entries before filtering.</summary>
    public int MaxSitemapEntries { get; set; } = 10_000;

    /// <summary>Gets or sets the maximum number of pages accumulated across all sources.</summary>
    public int MaxCrawlPages { get; set; } = 10_000;

    /// <summary>Gets or sets the maximum aggregate UTF-8 Markdown bytes across all sources.</summary>
    public long MaxCrawlBytes { get; set; } = 67_108_864;

    /// <summary>Gets or sets the maximum chunks accumulated, including unchanged chunks.</summary>
    public int MaxCrawlChunks { get; set; } = 100_000;

    /// <summary>Gets whether crawl limits are within supported memory and response bounds.</summary>
    public bool CrawlLimitsAreValid =>
        MaxSitemapBytes is >= 65_536 and <= 52_428_800 && MaxSitemapEntries is >= 1 and <= 50_000 &&
        MaxCrawlPages is >= 1 and <= 100_000 && MaxCrawlBytes is >= 1_048_576 and <= 268_435_456 &&
        MaxCrawlChunks is >= 1 and <= 1_000_000;

    /// <summary>Gets or sets the estimated-token admission cap for an incremental pass.</summary>
    public long MaxIncrementalTokens { get; set; } = 4_000_000;

    /// <summary>Gets or sets the estimated-token admission cap for an explicitly confirmed full pass.</summary>
    public long MaxFullTokens { get; set; } = 16_000_000;

    /// <summary>Gets or sets the verified model price per million input tokens. Zero disables paid indexing.</summary>
    public decimal PricePerMillionTokens { get; set; }

    /// <summary>Gets or sets the per-run dollar ceiling. Zero disables paid indexing.</summary>
    public decimal MaxRunDollars { get; set; }

    /// <summary>Gets or sets the full-run dollar ceiling, which also caps the UTC-day total on full-run days. Zero disables full paid indexing.</summary>
    public decimal MaxFullRunDollars { get; set; }

    /// <summary>Gets or sets the shared UTC-day dollar ceiling. Zero disables paid indexing.</summary>
    public decimal MaxDailyDollars { get; set; }

    /// <summary>Gets or sets the company and product site source options.</summary>
    public SitesOptions Sites { get; set; } = new();

    /// <summary>Gets or sets the public README source options.</summary>
    public ReadmesOptions Readmes { get; set; } = new();
}
