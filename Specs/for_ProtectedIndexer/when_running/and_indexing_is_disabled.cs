// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Cratis.Prompter.Protection;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ProtectedIndexer.when_running;

public class and_indexing_is_disabled : for_Indexer.given.an_indexer
{
    void Establish() => Options.Protection.IndexingEnabled = false;

    async Task Because()
    {
        var indexer = new ProtectedIndexer(Indexer, Microsoft.Extensions.Options.Options.Create(Options));
        Error = await Catch.Exception(() => indexer.Run(new IndexRequest(true, "FULL REINDEX")));
    }

    [Fact] void should_refuse_the_explicit_request() => Error.ShouldBeOfExactType<WorkRefused>();
    [Fact] void should_not_acquire_a_lease() => Chunks.DidNotReceive().AcquireIndexLease(Arg.Any<CancellationToken>());
    [Fact] void should_not_embed() => Embeddings.BatchSizes.ShouldBeEmpty();
}
