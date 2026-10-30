// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Indexer.when_running;

public class and_full_consent_is_missing : given.an_indexer
{
    async Task Because() => Error = await Catch.Exception(async () => await Indexer.Run(new IndexRequest(Full: true)));

    [Fact] void should_require_the_literal_confirmation() => Error.ShouldBeOfExactType<IndexConsentRequired>();
    [Fact] void should_not_acquire_the_lease() => Chunks.DidNotReceive().AcquireIndexLease(Arg.Any<CancellationToken>());
    [Fact] void should_not_dispatch() => Embeddings.BatchSizes.Count.ShouldEqual(0);
}
