// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Indexer.when_running;

public class and_the_lease_is_busy : given.an_indexer
{
    void Establish() => Chunks.AcquireIndexLease(Arg.Any<CancellationToken>()).Returns<IIndexLease>(_ => throw new IndexAlreadyRunning());
    async Task Because() => Error = await Catch.Exception(async () => await Indexer.Run());

    [Fact] void should_reject_without_waiting() => Error.ShouldBeOfExactType<IndexAlreadyRunning>();
    [Fact] void should_not_read_the_corpus() => Chunks.DidNotReceive().GetHashes(Arg.Any<CancellationToken>());
    [Fact] void should_not_call_the_provider() => Embeddings.BatchSizes.Count.ShouldEqual(0);
}
