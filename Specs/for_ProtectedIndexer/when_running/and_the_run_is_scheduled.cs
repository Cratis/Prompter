// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Cratis.Prompter.Protection;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ProtectedIndexer.when_running;

public class and_the_run_is_scheduled : for_Indexer.given.an_indexer
{
    async Task Because()
    {
        var indexer = new ProtectedIndexer(Indexer, Microsoft.Extensions.Options.Options.Create(Options));
        await indexer.Run(new IndexRequest(Scheduled: true));
    }

    [Fact] void should_reserve_the_daily_scheduled_slot() => Lease.Received(1).Reserve(new IndexExposure(0, 0), Options.Ingestion.MaxDailyDollars, true, Arg.Any<CancellationToken>());
}
