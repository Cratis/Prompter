// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;
using Cratis.Prompter.Embeddings;
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
            await lease.Reserve(new(new(0, 0), options.Value.Ingestion.MaxDailyDollars, Scheduled: true, Full: false), cancellationToken);
        }

        var meter = new IndexMeter(lease, options.Value, request);
        var existing = await chunks.GetHashes(cancellationToken);
        var crawl = await Discover(existing, request, cancellationToken);
        var seen = crawl.Seen;
        var changed = crawl.Changed;
        var metadata = crawl.Metadata;

        var failed = docsSite.FailedSources;
        seen.UnionWith(existing.Keys.Where(id => failed.Any(prefix => id.Value.StartsWith(prefix, StringComparison.Ordinal))));
        var removed = 0;
        if (seen.Count == 0)
        {
            logger.SkippedRemovalForEmptyCrawl();
        }
        else
        {
            var budget = IndexBudget.Preflight(changed.Select(chunk => chunk.EmbeddingInput), options.Value, request);
            logger.IndexExposureReserved(budget.Tokens, budget.Dollars);
            var staged = await Embed(changed, meter, cancellationToken);
            var snapshot = new IndexSnapshot(staged, seen, metadata) { PreservedSources = failed.ToArray() };
            removed = await Publish(lease, snapshot, cancellationToken);
            logger.IndexSpendMetered(meter.Spent.Tokens, meter.Spent.Dollars);
        }

        var run = new IndexRun(crawl.Pages, changed.Count, crawl.Unchanged, removed, Stopwatch.GetElapsedTime(start));
        return Complete(run, failed);
    }

    static Task<int> Publish(IIndexLease lease, IndexSnapshot snapshot, CancellationToken cancellationToken) =>
        snapshot.PreservedSources.Count == 0
            ? lease.CommitIndex(snapshot.Changed, snapshot.Existing, snapshot.Pages, cancellationToken)
            : lease.CommitIndex(snapshot, cancellationToken);

    async Task<CrawlCandidates> Discover(IReadOnlyDictionary<ChunkId, ContentHash> existing, IndexRequest request, CancellationToken cancellationToken)
    {
        var crawl = new CrawlCandidates();
        await foreach (var page in docsSite.GetPages(cancellationToken))
        {
            crawl.Pages++;
            crawl.Bytes += Encoding.UTF8.GetByteCount(page.Markdown);
            CheckCrawlLimit(nameof(IngestionOptions.MaxCrawlPages), crawl.Pages, options.Value.Ingestion.MaxCrawlPages);
            CheckCrawlLimit(nameof(IngestionOptions.MaxCrawlBytes), crawl.Bytes, options.Value.Ingestion.MaxCrawlBytes);
            crawl.Metadata.Add(page.Metadata);
            foreach (var chunk in MarkdownChunker.Chunk(page.Url, page.Markdown)) AddCandidate(crawl, chunk, existing, request);
        }
        return crawl;
    }

    void AddCandidate(CrawlCandidates crawl, Chunk chunk, IReadOnlyDictionary<ChunkId, ContentHash> existing, IndexRequest request)
    {
        crawl.Chunks++;
        CheckCrawlLimit(nameof(IngestionOptions.MaxCrawlChunks), crawl.Chunks, options.Value.Ingestion.MaxCrawlChunks);
        crawl.Seen.Add(chunk.Id);
        var sameHash = existing.TryGetValue(chunk.Id, out var hash) && hash == chunk.Hash;
        if (!request.Full && sameHash)
        {
            crawl.Unchanged++;
            return;
        }

        crawl.Tokens += IndexBudget.EstimatedTokensFor(chunk.EmbeddingInput);
        var limit = request.Full ? options.Value.Ingestion.MaxFullTokens : options.Value.Ingestion.MaxIncrementalTokens;
        if (crawl.Tokens > limit)
        {
            logger.CrawlLimitExceeded(request.Full ? nameof(IngestionOptions.MaxFullTokens) : nameof(IngestionOptions.MaxIncrementalTokens), limit);
            throw new IndexBudgetExceeded();
        }
        crawl.Changed.Add(chunk);
    }

    void CheckCrawlLimit(string name, long actual, long limit)
    {
        if (actual <= limit) return;
        logger.CrawlLimitExceeded(name, limit);
        throw new IncompleteCrawl();
    }

    IndexRun Complete(IndexRun run, IReadOnlyCollection<string> failed)
    {
        if (failed.Count > 0) throw new DegradedIndex(run, failed.ToArray());
        logger.IndexRunCompleted(run.Pages, run.Embedded, run.Unchanged, run.Removed, run.Duration);
        return run;
    }

    async Task<IReadOnlyCollection<EmbeddedChunk>> Embed(IReadOnlyList<Chunk> changed, IndexMeter meter, CancellationToken cancellationToken)
    {
        var staged = new List<EmbeddedChunk>();
        var batch = new List<Chunk>();
        var batchTokens = 0L;
        foreach (var chunk in changed)
        {
            var tokens = IndexBudget.TokensFor(chunk.EmbeddingInput);
            var tooManyInputs = batch.Count >= Math.Clamp(options.Value.Voyage.BatchSize, 1, EmbeddingInputLimits.MaximumItems);
            var tooManyTokens = batchTokens + tokens > EmbeddingInputLimits.MaximumRequestTokens;
            var needsFlush = tooManyInputs || tooManyTokens;
            if (batch.Count > 0 && needsFlush)
            {
                await Flush(batch, staged, meter, cancellationToken);
                batchTokens = 0;
            }

            batch.Add(chunk);
            batchTokens += tokens;
        }

        await Flush(batch, staged, meter, cancellationToken);
        return staged;
    }

    async Task Flush(List<Chunk> batch, List<EmbeddedChunk> staged, IndexMeter meter, CancellationToken cancellationToken)
    {
        if (batch.Count == 0) return;
        var inputs = batch.Select(chunk => chunk.EmbeddingInput).ToArray();
        var bound = IndexBudget.Batch(inputs, options.Value);
        await meter.Reserve(bound, cancellationToken);
        logger.IndexBatchStarted(batch.Count, staged.Count);
        try
        {
            var generated = await embeddings.GenerateAsync(
                inputs,
                new() { AdditionalProperties = new() { [EmbeddingRetry.TaskKey] = "index" } },
                cancellationToken);
            var attempts = generated.AdditionalProperties?.TryGetValue(EmbeddingRetry.AttemptsKey, out var count) == true && count is int value ? value : 0;
            var used = IndexBudget.Used(inputs.Sum(IndexBudget.TokensFor), attempts, generated.Usage?.InputTokenCount, options.Value);
            if (!await meter.Settle(bound, used)) logger.IndexReleaseKept();
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

    sealed class CrawlCandidates
    {
        public int Pages { get; set; }
        public long Bytes { get; set; }
        public int Chunks { get; set; }
        public int Unchanged { get; set; }
        public long Tokens { get; set; }
        public List<Chunk> Changed { get; } = [];
        public HashSet<ChunkId> Seen { get; } = [];
        public List<DocumentationMetadata> Metadata { get; } = [];
    }
}
