// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// Represents an implementation of <see cref="IIndexer"/>.
/// </summary>
/// <param name="docsSite">The documentation site to ingest from.</param>
/// <param name="chunks">The stored chunks.</param>
/// <param name="embeddings">The generator used to embed chunk content.</param>
/// <param name="options">The Prompter options carrying the embedding batch size.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class Indexer(
    IDocsSite docsSite,
    IChunks chunks,
    IEmbeddingGenerator<string, Embedding<float>> embeddings,
    IOptions<PrompterOptions> options,
    ILogger<Indexer> logger) : IIndexer
{
    /// <summary>
    /// Character budget per embeddings request. Voyage caps a single voyage-4 request at 320K tokens; a batch
    /// this size stays well under that even for the largest chunks, but the guard flushes early if a batch's
    /// characters ever approach the token cap.
    /// </summary>
    const int MaxBatchCharacters = 800_000;

    /// <inheritdoc/>
    public async Task<IndexRun> Run(CancellationToken cancellationToken = default)
    {
        var start = Stopwatch.GetTimestamp();
        var batchSize = Math.Max(1, options.Value.Voyage.BatchSize);
        var existing = await chunks.GetHashes(cancellationToken);
        var seen = new HashSet<ChunkId>();
        var pending = new List<Chunk>(batchSize);
        var pendingInputs = new List<string>(batchSize);
        var pendingCharacters = 0;
        var pages = 0;
        var embedded = 0;
        var unchanged = 0;

        async Task Flush()
        {
            if (pending.Count == 0)
            {
                return;
            }

            var generated = await embeddings.GenerateAsync(pendingInputs, cancellationToken: cancellationToken);
            for (var index = 0; index < pending.Count; index++)
            {
                await chunks.Upsert(pending[index], generated[index].Vector, cancellationToken);
            }

            embedded += pending.Count;
            pending.Clear();
            pendingInputs.Clear();
            pendingCharacters = 0;
        }

        await foreach (var page in docsSite.GetPages(cancellationToken))
        {
            pages++;

            foreach (var chunk in MarkdownChunker.Chunk(page.Url, page.Markdown))
            {
                seen.Add(chunk.Id);

                if (existing.TryGetValue(chunk.Id, out var hash) && hash == chunk.Hash)
                {
                    unchanged++;
                    continue;
                }

                var input = $"{chunk.Title} — {chunk.HeadingPath}\n\n{chunk.Content}";
                if (pending.Count > 0 && (pending.Count >= batchSize || pendingCharacters + input.Length > MaxBatchCharacters))
                {
                    await Flush();
                }

                pending.Add(chunk);
                pendingInputs.Add(input);
                pendingCharacters += input.Length;
            }
        }

        await Flush();

        var removed = await chunks.RemoveAllExcept(seen, cancellationToken);
        var run = new IndexRun(pages, embedded, unchanged, removed, Stopwatch.GetElapsedTime(start));
        logger.IndexRunCompleted(run.Pages, run.Embedded, run.Unchanged, run.Removed, run.Duration);

        return run;
    }
}
