// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Cratis.Prompter.Specs.Fakes;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Indexer.when_running;

public class and_changed_chunks_exceed_the_batch_size : Specification
{
    RecordingEmbeddingGenerator _embeddings = null!;
    IChunks _chunks = null!;
    Indexer _indexer = null!;
    IndexRun _run = null!;

    void Establish()
    {
        var pages = Enumerable.Range(0, 5)
            .Select(index => new DocsPage(new PageUrl($"https://cratis.io/page-{index}.md"), $"# Page {index}\n\nBody for page {index}."))
            .ToArray();

        _embeddings = new RecordingEmbeddingGenerator();
        _chunks = Substitute.For<IChunks>();
        _chunks.GetHashes(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<ChunkId, ContentHash>());
        _chunks.RemoveAllExcept(Arg.Any<IReadOnlySet<ChunkId>>(), Arg.Any<CancellationToken>()).Returns(0);

        var options = Options.Create(new PrompterOptions { Voyage = { BatchSize = 2 } });
        _indexer = new Indexer(new FakeDocsSite(pages), _chunks, _embeddings, options, NullLogger<Indexer>.Instance);
    }

    void Because() => _run = _indexer.Run().GetAwaiter().GetResult();

    [Fact] void should_embed_every_changed_chunk() => _run.Embedded.ShouldEqual(5);
    [Fact] void should_report_every_page() => _run.Pages.ShouldEqual(5);
    [Fact] void should_report_no_unchanged_chunks() => _run.Unchanged.ShouldEqual(0);
    [Fact] void should_never_exceed_the_batch_size() => _embeddings.BatchSizes.All(size => size <= 2).ShouldBeTrue();
    [Fact] void should_split_the_work_into_batches() => _embeddings.BatchSizes.Count.ShouldEqual(3);

    [Fact]
    void should_upsert_every_chunk() =>
        _chunks.Received(5).Upsert(Arg.Any<Chunk>(), Arg.Any<ReadOnlyMemory<float>>(), Arg.Any<CancellationToken>());
}
