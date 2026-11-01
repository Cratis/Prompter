// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_IndexMeter.when_metering;

public class and_a_retry_preceded_success : Specification
{
    readonly IIndexLease _lease = Substitute.For<IIndexLease>();
    IndexMeter _meter = null!;
    async Task Because()
    {
        var options = new PrompterOptions { Ingestion = { PricePerMillionTokens = 1, MaxRunDollars = 1, MaxDailyDollars = 1 } };
        _meter = new(_lease, options, new());
        var batch = IndexBudget.Batch(["text"], options);
        await _meter.Reserve(batch, CancellationToken.None);
        await _meter.Settle(batch, IndexBudget.Used(68, 2, 2, options));
    }

    [Fact] void should_count_the_failed_attempt_at_its_bound() => _meter.Spent.Tokens.ShouldEqual(70L);
    [Fact] async Task should_release_only_the_measured_surplus() => await _lease.Received(1).Release(new(338, 0.000338m), Arg.Any<CancellationToken>());
}
