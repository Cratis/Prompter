// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Indexer.when_running;

public class and_a_later_batch_fails : given.an_indexer
{
    void Establish()
    {
        Embeddings.Attempts = 1;
        Embeddings.UsageFor = inputs => 1;
        Embeddings.BeforeGenerate = count =>
        {
            if (count == 2) throw new IndexBudgetExceeded();
        };
    }

    async Task Because() => Error = await Catch.Exception(async () => await Indexer.Run());

    [Fact] void should_fail_instead_of_reporting_success() => Error.ShouldBeOfExactType<IndexBudgetExceeded>();
    [Fact] void should_not_publish_any_batch() => Lease.DidNotReceive().CommitIndex(Arg.Any<IReadOnlyCollection<EmbeddedChunk>>(), Arg.Any<IReadOnlySet<ChunkId>>(), Arg.Any<IReadOnlyCollection<DocumentationMetadata>>(), Arg.Any<CancellationToken>());
    [Fact]
    async Task should_release_only_the_successful_batch()
    {
        var chunk = MarkdownChunker.Chunk(new PageUrl("https://cratis.io/a.md"), "# A\n\nAlpha.").Single();
        var reserved = IndexBudget.Batch([chunk.EmbeddingInput], Options);
        await Lease.Received(1).Release(Arg.Any<IndexExposure>(), Arg.Any<CancellationToken>());
        await Lease.Received(1).Release(new(reserved.Tokens - 1, reserved.Dollars - 0.000001m), Arg.Any<CancellationToken>());
    }

    [Fact] async Task should_release_the_lease() => await Lease.Received(1).DisposeAsync();
}
