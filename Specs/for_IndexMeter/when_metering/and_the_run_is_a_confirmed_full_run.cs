// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_IndexMeter.when_metering;

public class and_the_run_is_a_confirmed_full_run : Specification
{
    readonly IIndexLease _lease = Substitute.For<IIndexLease>();
    readonly PrompterOptions _options = new() { Ingestion = { PricePerMillionTokens = 1, MaxFullRunDollars = 1, MaxRunDollars = 0.25m, MaxDailyDollars = 0.5m } };
    IndexMeter _meter = null!;
    IndexExposure _batch = null!;

    void Establish()
    {
        _meter = new(_lease, _options, new(true, "FULL REINDEX"));
        _batch = IndexBudget.Batch(["text"], _options);
    }

    async Task Because() => await _meter.Reserve(_batch, CancellationToken.None);

    [Fact]
    async Task should_record_the_confirmed_full_run_with_its_ceiling() =>
        await _lease.Received(1).Reserve(new(_batch, 1m, Scheduled: false, Full: true), CancellationToken.None);
}
