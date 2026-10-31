// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Indexer.when_running;

public class and_the_daily_reservation_is_rejected : given.an_indexer
{
    void Establish() => Lease.Reserve(Arg.Any<IndexExposure>(), Arg.Any<decimal>(), true, Arg.Any<CancellationToken>()).Returns(_ => throw new IndexBudgetExceeded());
    async Task Because() => Error = await Catch.Exception(async () => await Indexer.Run(new IndexRequest(Scheduled: true)));

    [Fact] void should_fail_closed() => Error.ShouldBeOfExactType<IndexBudgetExceeded>();
    [Fact] void should_dispatch_nothing() => Embeddings.BatchSizes.Count.ShouldEqual(0);
    [Fact] async Task should_release_the_lease() => await Lease.Received(1).DisposeAsync();
}
