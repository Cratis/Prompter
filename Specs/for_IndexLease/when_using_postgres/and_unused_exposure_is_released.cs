// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_IndexLease.when_using_postgres;

[Trait("Category", "Integration")]
[Collection("IndexPostgres")]
public class and_unused_exposure_is_released : given.a_database
{
    Exception? _afterRelease;
    Exception? _overdraft;
    Exception? _otherLease;
    Exception? _secondSchedule;

    async Task Because()
    {
        await using (var first = await IndexLease.Acquire(Database, CancellationToken.None))
        {
            await first.Reserve(new(200, 0.2m), 0.3m, true, CancellationToken.None);
            await first.Release(new(150, 0.15m), CancellationToken.None);
            _afterRelease = await Catch.Exception(() => first.Reserve(new(250, 0.25m), 0.3m, false, CancellationToken.None));
            await first.Release(new(1_000, 1m), CancellationToken.None);
            await first.Release(new(1_000, 1m), CancellationToken.None);
            _overdraft = await Catch.Exception(() => first.Reserve(new(301, 0.301m), 0.3m, false, CancellationToken.None));
            await first.Reserve(new(100, 0.1m), 0.3m, false, CancellationToken.None);
        }

        await using var next = await IndexLease.Acquire(Database, CancellationToken.None);
        await next.Release(new(1_000, 1m), CancellationToken.None);
        _otherLease = await Catch.Exception(() => next.Reserve(new(201, 0.201m), 0.3m, false, CancellationToken.None));
        _secondSchedule = await Catch.Exception(() => next.Reserve(new(0, 0), 0.3m, true, CancellationToken.None));
    }

    [Fact] void should_admit_the_next_batch_after_releasing_surplus() => _afterRelease.ShouldBeNull();
    [Fact] void should_never_release_below_zero() => _overdraft.ShouldBeOfExactType<IndexBudgetExceeded>();
    [Fact] void should_never_release_another_leases_reservation() => _otherLease.ShouldBeOfExactType<IndexBudgetExceeded>();
    [Fact] void should_preserve_the_scheduled_slot() => _secondSchedule.ShouldBeOfExactType<IndexBudgetExceeded>();
}
