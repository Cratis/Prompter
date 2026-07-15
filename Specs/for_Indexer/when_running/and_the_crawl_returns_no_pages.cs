// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Cratis.Prompter.Specs.Fakes;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Indexer.when_running;

public class and_the_crawl_returns_no_pages : Specification
{
    RecordingEmbeddingGenerator _embeddings = null!;
    IChunks _chunks = null!;
    Indexer _indexer = null!;
    IndexRun _run = null!;

    void Establish()
    {
        var existing = MarkdownChunker.Chunk(
            new PageUrl("https://cratis.io/guide.md"),
            "# Guide\n\n## Alpha\n\nAlpha body.").ToArray();

        _embeddings = new RecordingEmbeddingGenerator();
        _chunks = Substitute.For<IChunks>();
        _chunks.GetHashes(Arg.Any<CancellationToken>())
            .Returns(existing.ToDictionary(chunk => chunk.Id, chunk => chunk.Hash));

        var options = Options.Create(new PrompterOptions());
        _indexer = new Indexer(new FakeDocsSite([]), _chunks, _embeddings, options, NullLogger<Indexer>.Instance);
    }

    void Because() => _run = _indexer.Run().GetAwaiter().GetResult();

    [Fact] void should_report_no_pages() => _run.Pages.ShouldEqual(0);
    [Fact] void should_report_nothing_removed() => _run.Removed.ShouldEqual(0);

    [Fact]
    void should_not_remove_the_existing_corpus() =>
        _chunks.DidNotReceive().RemoveAllExcept(Arg.Any<IReadOnlySet<ChunkId>>(), Arg.Any<CancellationToken>());
}
