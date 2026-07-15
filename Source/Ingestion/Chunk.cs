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
public record Chunk(ChunkId Id, PageUrl Page, string Title, string HeadingPath, string Content, ContentHash Hash);
