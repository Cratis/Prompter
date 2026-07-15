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
}
