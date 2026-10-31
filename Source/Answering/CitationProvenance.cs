// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Prompter.Answering;

/// <summary>
/// Represents the honest provenance of one cited page, taken from the index and never from model output.
/// </summary>
/// <param name="Page">The cited page.</param>
/// <param name="IndexedAt">When the page was last published by a successful index run; <see langword="null"/> when unknown.</param>
/// <param name="PageLastModifiedAt">The page's explicit source last-modified date; retained, not rendered by default.</param>
/// <param name="ProductVersion">The explicit product version the page stated, without a leading v.</param>
public record CitationProvenance(PageUrl Page, DateTimeOffset? IndexedAt, DateTimeOffset? PageLastModifiedAt, string? ProductVersion)
{
    /// <summary>Gets the compact note for a source line, e.g. <c language="text">indexed 2026-10-30 · v16.0.0</c>.</summary>
    public string Note =>
        (IndexedAt is { } indexed
            ? $"indexed {indexed.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}"
            : "index date unknown")
        + (string.IsNullOrWhiteSpace(ProductVersion) ? string.Empty : $" · v{ProductVersion}");
}
