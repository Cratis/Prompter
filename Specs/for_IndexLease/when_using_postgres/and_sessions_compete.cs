// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_IndexLease.when_using_postgres;

[Trait("Category", "Integration")]
[Collection("IndexPostgres")]
public class and_sessions_compete : given.a_database
{
    Exception? _contention;
    Exception? _secondSchedule;
    Exception? _dailyCap;
    bool _reacquired;
    int _removed;

    async Task Because()
    {
        await using (var first = await IndexLease.Acquire(Database, CancellationToken.None))
        {
            _contention = await Catch.Exception(async () => await IndexLease.Acquire(Database, CancellationToken.None));
            await first.Reserve(new(100, 0.1m), 0.2m, true, CancellationToken.None);
        }

        await using var next = await IndexLease.Acquire(Database, CancellationToken.None);
        _reacquired = true;
        _secondSchedule = await Catch.Exception(async () => await next.Reserve(new(0, 0), 0.2m, true, CancellationToken.None));
        _dailyCap = await Catch.Exception(async () => await next.Reserve(new(100, 0.11m), 0.2m, false, CancellationToken.None));
        _removed = await next.CommitIndex([], new HashSet<ChunkId> { new("keep") }, [], CancellationToken.None);
    }

    [Fact] void should_reject_a_competing_session() => _contention.ShouldBeOfExactType<IndexAlreadyRunning>();
    [Fact] void should_release_and_reacquire() => _reacquired.ShouldBeTrue();
    [Fact] void should_keep_the_daily_slot_after_releasing_the_lock() => _secondSchedule.ShouldBeOfExactType<IndexBudgetExceeded>();
    [Fact] void should_keep_the_dollar_reservation() => _dailyCap.ShouldBeOfExactType<IndexBudgetExceeded>();
    [Fact] void should_publish_the_completed_snapshot() => _removed.ShouldEqual(1);
}
