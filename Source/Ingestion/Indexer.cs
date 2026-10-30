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
/// <param name="options">The ingestion and embedding limits.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class Indexer(
    IDocsSite docsSite,
    IChunks chunks,
    IEmbeddingGenerator<string, Embedding<float>> embeddings,
    IOptions<PrompterOptions> options,
    ILogger<Indexer> logger) : IIndexer
{
    const long MaxBatchTokens = 300_000;

    /// <inheritdoc/>
    public Task<IndexRun> Run(CancellationToken cancellationToken = default) => Run(new IndexRequest(), cancellationToken);

    /// <inheritdoc/>
    public async Task<IndexRun> Run(IndexRequest request, CancellationToken cancellationToken = default)
    {
        request.Validate();
        await using var lease = await chunks.AcquireIndexLease(cancellationToken);
        var start = Stopwatch.GetTimestamp();
        var profile = $"{options.Value.Voyage.Model}/{options.Value.Voyage.Dimensions}/markdown-v1";
        await lease.CheckProfile(profile, request.Full, cancellationToken);
        if (request.Scheduled)
        {
            await lease.Reserve(new(0, 0), options.Value.Ingestion.MaxDailyDollars, true, cancellationToken);
        }

        var existing = await chunks.GetHashes(cancellationToken);
        var seen = new HashSet<ChunkId>();
        var changed = new List<Chunk>();
        var pages = 0;
        var unchanged = 0;

        await foreach (var page in docsSite.GetPages(cancellationToken))
        {
            pages++;
            foreach (var chunk in MarkdownChunker.Chunk(page.Url, page.Markdown))
            {
                seen.Add(chunk.Id);
                var sameHash = existing.TryGetValue(chunk.Id, out var hash) && hash == chunk.Hash;
                if (!request.Full && sameHash)
                {
                    unchanged++;
                }
                else
                {
                    changed.Add(chunk);
                }
            }
        }

        var removed = 0;
        if (seen.Count == 0)
        {
            logger.SkippedRemovalForEmptyCrawl();
        }
        else
        {
            var budget = IndexBudget.Preflight(changed.Select(chunk => chunk.EmbeddingInput), options.Value, request);
            await lease.Reserve(budget, options.Value.Ingestion.MaxDailyDollars, false, cancellationToken);
            logger.IndexExposureReserved(budget.Tokens, budget.Dollars);
            var staged = await Embed(changed, lease, cancellationToken);
            removed = await lease.CommitIndex(staged, seen, cancellationToken);
        }

        var run = new IndexRun(pages, changed.Count, unchanged, removed, Stopwatch.GetElapsedTime(start));
        logger.IndexRunCompleted(run.Pages, run.Embedded, run.Unchanged, run.Removed, run.Duration);
        return run;
    }

    async Task<IReadOnlyCollection<EmbeddedChunk>> Embed(IReadOnlyList<Chunk> changed, IIndexLease lease, CancellationToken cancellationToken)
    {
        var staged = new List<EmbeddedChunk>();
        var batch = new List<Chunk>();
        var batchTokens = 0L;
        foreach (var chunk in changed)
        {
            var tokens = IndexBudget.TokensFor(chunk.EmbeddingInput);
            var tooManyInputs = batch.Count >= Math.Clamp(options.Value.Voyage.BatchSize, 1, 1000);
            var tooManyTokens = batchTokens + tokens > MaxBatchTokens;
            var needsFlush = tooManyInputs || tooManyTokens;
            if (batch.Count > 0 && needsFlush)
            {
                await Flush(batch, staged, lease, cancellationToken);
                batchTokens = 0;
            }

            batch.Add(chunk);
            batchTokens += tokens;
        }

        await Flush(batch, staged, lease, cancellationToken);
        return staged;
    }

    async Task Flush(List<Chunk> batch, List<EmbeddedChunk> staged, IIndexLease lease, CancellationToken cancellationToken)
    {
        if (batch.Count == 0) return;
        await lease.EnsureHeld(cancellationToken);
        logger.IndexBatchStarted(batch.Count, staged.Count);
        try
        {
            var generated = await embeddings.GenerateAsync(batch.Select(chunk => chunk.EmbeddingInput).ToArray(), cancellationToken: cancellationToken);
            for (var index = 0; index < batch.Count; index++)
            {
                staged.Add(new(batch[index], generated[index].Vector));
            }
        }
        catch
        {
            logger.IndexBatchFailed(batch.Count, staged.Count);
            throw;
        }

        batch.Clear();
    }
}
