// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Indexer.when_running;

public class and_cancellation_interrupts_embedding : given.an_indexer
{
    void Establish() => Embeddings.BeforeGenerate = _ => throw new OperationCanceledException();
    async Task Because() => Error = await Catch.Exception(async () => await Indexer.Run());

    [Fact] void should_propagate_cancellation() => Error.ShouldBeOfExactType<OperationCanceledException>();
    [Fact] void should_preserve_the_corpus() => Lease.DidNotReceive().CommitIndex(Arg.Any<IReadOnlyCollection<EmbeddedChunk>>(), Arg.Any<IReadOnlySet<ChunkId>>(), Arg.Any<IReadOnlyCollection<DocumentationMetadata>>(), Arg.Any<CancellationToken>());
    [Fact] async Task should_release_the_lease() => await Lease.Received(1).DisposeAsync();
}
