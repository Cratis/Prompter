// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// Represents an implementation of <see cref="IIndexer"/>.
/// </summary>
/// <param name="docsSite">The documentation site to ingest from.</param>
/// <param name="chunks">The stored chunks.</param>
/// <param name="embeddings">The generator used to embed chunk content.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class Indexer(
    IDocsSite docsSite,
    IChunks chunks,
    IEmbeddingGenerator<string, Embedding<float>> embeddings,
    ILogger<Indexer> logger) : IIndexer
{
    /// <inheritdoc/>
    public async Task Run(CancellationToken cancellationToken = default)
    {
        var existing = await chunks.GetHashes(cancellationToken);
        var seen = new HashSet<ChunkId>();
        var embedded = 0;
        var unchanged = 0;

        await foreach (var page in docsSite.GetPages(cancellationToken))
        {
            foreach (var chunk in MarkdownChunker.Chunk(page.Url, page.Markdown))
            {
                seen.Add(chunk.Id);

                if (existing.TryGetValue(chunk.Id, out var hash) && hash == chunk.Hash)
                {
                    unchanged++;
                    continue;
                }

                var contextualContent = $"{chunk.Title} — {chunk.HeadingPath}\n\n{chunk.Content}";
                var generated = await embeddings.GenerateAsync([contextualContent], cancellationToken: cancellationToken);
                await chunks.Upsert(chunk, generated[0].Vector, cancellationToken);
                embedded++;
            }
        }

        var removed = await chunks.RemoveAllExcept(seen, cancellationToken);
        logger.IndexRunCompleted(embedded, unchanged, removed);
    }
}
