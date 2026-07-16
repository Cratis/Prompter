// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// Represents a retrievable piece of a documentation page.
/// </summary>
/// <param name="Id">The deterministic identifier of the chunk.</param>
/// <param name="Page">The URL of the page the chunk belongs to.</param>
/// <param name="Title">The title of the page.</param>
/// <param name="HeadingPath">The heading path within the page, e.g. "Getting started &gt; Install".</param>
/// <param name="Content">The markdown content of the chunk.</param>
/// <param name="Hash">The content hash used for incremental re-indexing.</param>
public record Chunk(ChunkId Id, PageUrl Page, string Title, string HeadingPath, string Content, ContentHash Hash)
{
    /// <summary>
    /// Gets the string that is embedded for (and stored against) this chunk - see
    /// <see cref="EmbeddingInputFor(string, string, string)"/>.
    /// </summary>
    public string EmbeddingInput => EmbeddingInputFor(Title, HeadingPath, Content);

    /// <summary>
    /// Builds the string that is embedded for (and stored against) a chunk: the title and heading path give the
    /// body its documentation context, so a passage retrieves and cites correctly even when the body alone is
    /// ambiguous. This is the single source of truth shared by the chunker (which hashes it for change
    /// detection) and the indexer (which embeds it), so a title or heading rename re-embeds instead of being
    /// skipped as unchanged, and the hash can never cover less than what is embedded.
    /// </summary>
    /// <param name="title">The title of the page.</param>
    /// <param name="headingPath">The heading path within the page.</param>
    /// <param name="content">The markdown content of the chunk.</param>
    /// <returns>The composite string to embed and hash.</returns>
    public static string EmbeddingInputFor(string title, string headingPath, string content) =>
        $"{title} — {headingPath}\n\n{content}";
}
