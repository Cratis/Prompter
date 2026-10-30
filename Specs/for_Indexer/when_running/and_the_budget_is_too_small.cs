// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Indexer.when_running;

public class and_the_budget_is_too_small : given.an_indexer
{
    void Establish() => Options.Ingestion.MaxIncrementalTokens = 1;
    async Task Because() => Error = await Catch.Exception(async () => await Indexer.Run());

    [Fact] void should_fail_the_run() => Error.ShouldBeOfExactType<IndexBudgetExceeded>();
    [Fact] void should_not_dispatch_embeddings() => Embeddings.BatchSizes.Count.ShouldEqual(0);
    [Fact] async Task should_release_the_lease() => await Lease.Received(1).DisposeAsync();
    [Fact] void should_not_change_the_corpus() => Lease.DidNotReceive().CommitIndex(Arg.Any<IReadOnlyCollection<EmbeddedChunk>>(), Arg.Any<IReadOnlySet<ChunkId>>(), Arg.Any<CancellationToken>());
}
