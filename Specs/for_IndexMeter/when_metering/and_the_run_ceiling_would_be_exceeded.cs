// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_IndexMeter.when_metering;

public class and_the_run_ceiling_would_be_exceeded : Specification
{
    readonly IIndexLease _lease = Substitute.For<IIndexLease>();
    Exception? _error;
    async Task Because()
    {
        var meter = new IndexMeter(_lease, new() { Ingestion = { MaxRunDollars = 0.25m } }, new());
        await meter.Reserve(new(1, 0.2m), CancellationToken.None);
        _lease.ClearReceivedCalls();
        _error = await Catch.Exception(() => meter.Reserve(new(1, 0.1m), CancellationToken.None));
    }

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<IndexBudgetExceeded>();
    [Fact] async Task should_not_reserve() => await _lease.DidNotReceive().Reserve(Arg.Any<IndexExposure>(), Arg.Any<decimal>(), Arg.Any<bool>(), full: Arg.Any<bool>(), Arg.Any<CancellationToken>());
}
