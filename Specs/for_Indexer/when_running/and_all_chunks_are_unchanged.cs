// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Cratis.Prompter.Specs.Fakes;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Indexer.when_running;

public class and_all_chunks_are_unchanged : Specification
{
    const string Markdown = "# Guide\n\n## Alpha\n\nAlpha body.\n\n## Beta\n\nBeta body.";

    RecordingEmbeddingGenerator _embeddings = null!;
    IChunks _chunks = null!;
    Indexer _indexer = null!;
    IndexRun _run = null!;
    int _chunkCount;

    void Establish()
    {
        var url = new PageUrl("https://cratis.io/guide.md");
        var existing = MarkdownChunker.Chunk(url, Markdown).ToArray();
        _chunkCount = existing.Length;

        _embeddings = new RecordingEmbeddingGenerator();
        _chunks = Substitute.For<IChunks>();
        _chunks.GetHashes(Arg.Any<CancellationToken>())
            .Returns(existing.ToDictionary(chunk => chunk.Id, chunk => chunk.Hash));
        _chunks.RemoveAllExcept(Arg.Any<IReadOnlySet<ChunkId>>(), Arg.Any<CancellationToken>()).Returns(0);

        var options = Options.Create(new PrompterOptions());
        _indexer = new Indexer(new FakeDocsSite([new DocsPage(url, Markdown)]), _chunks, _embeddings, options, NullLogger<Indexer>.Instance);
    }

    void Because() => _run = _indexer.Run().GetAwaiter().GetResult();

    [Fact] void should_produce_more_than_one_chunk() => _chunkCount.ShouldEqual(2);
    [Fact] void should_embed_nothing() => _run.Embedded.ShouldEqual(0);
    [Fact] void should_report_all_chunks_unchanged() => _run.Unchanged.ShouldEqual(2);
    [Fact] void should_not_call_the_embedding_generator() => _embeddings.BatchSizes.Count.ShouldEqual(0);

    [Fact]
    void should_not_upsert_anything() =>
        _chunks.DidNotReceive().Upsert(Arg.Any<Chunk>(), Arg.Any<ReadOnlyMemory<float>>(), Arg.Any<CancellationToken>());
}
