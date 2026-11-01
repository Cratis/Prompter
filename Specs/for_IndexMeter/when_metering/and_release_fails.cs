// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Npgsql;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_IndexMeter.when_metering;

public class and_release_fails : Specification
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    async Task should_report_the_kept_reservation(int failure)
    {
        var lease = Substitute.For<IIndexLease>();
        Exception error = failure switch { 0 => new NpgsqlException(), 1 => new TimeoutException(), _ => new OperationCanceledException() };
        lease.Release(Arg.Any<IndexExposure>(), Arg.Any<CancellationToken>()).Returns(_ => throw error);
        var meter = new IndexMeter(lease, new() { Ingestion = { MaxRunDollars = 1, MaxDailyDollars = 1 } }, new());
        await meter.Reserve(new(100, 0.1m), CancellationToken.None);
        (await meter.Settle(new(100, 0.1m), new(10, 0.01m))).ShouldBeFalse();
        meter.Spent.ShouldEqual(new IndexExposure(100, 0.1m));
    }
}
