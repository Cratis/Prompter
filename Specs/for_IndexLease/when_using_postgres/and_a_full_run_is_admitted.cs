// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_IndexLease.when_using_postgres;

[Trait("Category", "Integration")]
[Collection("IndexPostgres")]
public class and_a_full_run_is_admitted : given.a_database
{
    Exception? _overCeiling;
    decimal _incrementalCeiling;
    decimal _fullCeiling;

    async Task Because()
    {
        await using var lease = await IndexLease.Acquire(Database, CancellationToken.None);
        await lease.Reserve(new(new(100, 0.1m), 0.5m, Scheduled: false, Full: false), CancellationToken.None);
        await using var read = Database.CreateCommand("SELECT full_ceiling FROM index_reservations");
        _incrementalCeiling = (decimal)(await read.ExecuteScalarAsync())!;
        await lease.Reserve(new(new(500, 0.5m), 1m, Scheduled: false, Full: true), CancellationToken.None);
        _overCeiling = await Catch.Exception(() => lease.Reserve(new(new(500, 0.5m), 1m, Scheduled: false, Full: true), CancellationToken.None));
        _fullCeiling = (decimal)(await read.ExecuteScalarAsync())!;
    }

    [Fact] void should_leave_the_incremental_ceiling_unmarked() => _incrementalCeiling.ShouldEqual(0m);
    [Fact] void should_record_the_admitted_full_ceiling() => _fullCeiling.ShouldEqual(1m);
    [Fact] void should_reject_exposure_over_the_full_ceiling() => _overCeiling.ShouldBeOfExactType<IndexBudgetExceeded>();
}
