// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Cratis.Prompter.Specs.Fakes;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Indexer.when_running;

public class and_only_some_chunks_changed : given.an_indexer
{
    IEmbeddingGenerator<string, Embedding<float>> _generator = null!;
    Chunk _changed = null!;
    IndexRun _run = null!;
    int _unchanged;

    void Establish()
    {
        var first = new DocsPage(new PageUrl("https://cratis.io/a.md"), "# A\n\n## One\n\nAlpha.\n\n## Two\n\nBeta.");
        var second = new DocsPage(new PageUrl("https://cratis.io/b.md"), "# B\n\nChanged.");
        var existing = MarkdownChunker.Chunk(first.Url, first.Markdown).ToArray();
        _unchanged = existing.Length;
        _changed = MarkdownChunker.Chunk(second.Url, second.Markdown).Single();
        var hashes = existing.ToDictionary(chunk => chunk.Id, chunk => chunk.Hash);
        hashes[_changed.Id] = new ContentHash("old");
        hashes[new ChunkId("https://cratis.io/removed.md#0")] = new ContentHash("removed");
        Chunks.GetHashes(Arg.Any<CancellationToken>()).Returns(hashes);
        _generator = Substitute.For<IEmbeddingGenerator<string, Embedding<float>>>();
        _generator.GenerateAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<EmbeddingGenerationOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ => new GeneratedEmbeddings<Embedding<float>>([new(new float[4])]));
        Indexer = new Indexer(new FakeDocsSite([first, second]), Chunks, _generator, Microsoft.Extensions.Options.Options.Create(Options), NullLogger<Indexer>.Instance);
    }

    void Because() => _run = Indexer.Run().GetAwaiter().GetResult();

    [Fact] void should_embed_one_chunk() => _run.Embedded.ShouldEqual(1);
    [Fact] void should_leave_the_first_page_unchanged() => _run.Unchanged.ShouldEqual(_unchanged);
    [Fact] void should_reserve_only_changed_input_with_six_attempts() =>
        Lease.Received(1).Reserve(Arg.Is<IndexExposure>(exposure => exposure.Tokens == IndexBudget.TokensFor(_changed.EmbeddingInput) * 6), Options.Ingestion.MaxDailyDollars, false, Arg.Any<CancellationToken>());
    [Fact] void should_embed_only_the_changed_input() =>
        _generator.Received(1).GenerateAsync(Arg.Is<IEnumerable<string>>(inputs => inputs.SequenceEqual(new[] { _changed.EmbeddingInput })), Arg.Any<EmbeddingGenerationOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_commit_only_the_changed_chunk() =>
        Lease.Received(1).CommitIndex(Arg.Is<IReadOnlyCollection<EmbeddedChunk>>(chunks => chunks.Count == 1 && chunks.Single().Chunk == _changed), Arg.Any<IReadOnlySet<ChunkId>>(), Arg.Any<CancellationToken>());
}
