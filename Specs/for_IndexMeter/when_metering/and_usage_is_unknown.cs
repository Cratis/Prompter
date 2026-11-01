// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_IndexMeter.when_metering;

public class and_usage_is_unknown : Specification
{
    [Theory]
    [InlineData(1, null)]
    [InlineData(1, 69L)]
    [InlineData(1, -1L)]
    [InlineData(0, 2L)]
    [InlineData(7, 2L)]
    async Task should_keep_the_entire_reservation(int attempts, long? usage)
    {
        var lease = Substitute.For<IIndexLease>();
        var options = new PrompterOptions { Ingestion = { PricePerMillionTokens = 1, MaxRunDollars = 1, MaxDailyDollars = 1 } };
        var meter = new IndexMeter(lease, options, new());
        var batch = IndexBudget.Batch(["text"], options);
        await meter.Reserve(batch, CancellationToken.None);
        await meter.Settle(batch, IndexBudget.Used(68, attempts, usage, options));
        await lease.DidNotReceive().Release(Arg.Any<IndexExposure>(), Arg.Any<CancellationToken>());
        meter.Spent.ShouldEqual(batch);
    }
}
