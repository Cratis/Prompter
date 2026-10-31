// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter;

/// <summary>
/// Options controlling how the published documentation site is ingested into the retrieval corpus.
/// </summary>
public class IngestionOptions
{
    /// <summary>
    /// Gets or sets the path segments whose pages are excluded from ingestion. Any sitemap URL containing
    /// one of these segments is skipped. Defaults exclude Chronicle's generated client snippets and the
    /// generated API reference, neither of which grounds answers well (see D-9).
    /// </summary>
    public IList<string> ExcludedPathSegments { get; set; } = ["client-snippets", "api-reference"];

    /// <summary>Gets or sets the retry-inclusive token cap for an incremental pass.</summary>
    public long MaxIncrementalTokens { get; set; } = 1_000_000;

    /// <summary>Gets or sets the retry-inclusive token cap for an explicitly confirmed full pass.</summary>
    public long MaxFullTokens { get; set; } = 25_000_000;

    /// <summary>Gets or sets the verified model price per million input tokens. Zero disables paid indexing.</summary>
    public decimal PricePerMillionTokens { get; set; }

    /// <summary>Gets or sets the per-run dollar ceiling. Zero disables paid indexing.</summary>
    public decimal MaxRunDollars { get; set; }

    /// <summary>Gets or sets the shared UTC-day dollar ceiling. Zero disables paid indexing.</summary>
    public decimal MaxDailyDollars { get; set; }
}
