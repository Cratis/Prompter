// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_IndexMeter.when_metering;

public class and_usage_is_measured : Specification
{
    readonly IIndexLease _lease = Substitute.For<IIndexLease>();
    readonly PrompterOptions _options = new() { Ingestion = { PricePerMillionTokens = 1, MaxRunDollars = 1, MaxDailyDollars = 2 } };
    IndexMeter _meter = null!;
    IndexExposure _batch = null!;

    void Establish()
    {
        _meter = new(_lease, _options, new());
        _batch = IndexBudget.Batch(["text"], _options);
    }

    async Task Because()
    {
        await _meter.Reserve(_batch, CancellationToken.None);
        await _meter.Settle(_batch, IndexBudget.Used(68, 1, 2, _options));
    }

    [Fact] void should_reserve_before_settling() => Received.InOrder(() =>
    {
        _lease.Reserve(new(_batch, 2, Scheduled: false, Full: false), CancellationToken.None);
        _lease.Release(new(406, 0.000406m), Arg.Any<CancellationToken>());
    });
    [Fact] void should_count_measured_usage() => _meter.Spent.ShouldEqual(new IndexExposure(2, 0.000002m));
}
