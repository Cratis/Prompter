// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_MigrationPlan.when_selecting_pending;

public class and_some_versions_are_already_applied : Specification
{
    Migration[] _available = null!;
    IReadOnlyList<Migration> _pending = null!;

    void Establish() => _available =
    [
        new Migration(new MigrationVersion(1, 0, 0), "v1_0_0.sql", "-- baseline"),
        new Migration(new MigrationVersion(1, 1, 0), "v1_1_0.sql", "-- feedback"),
        new Migration(new MigrationVersion(2, 0, 0), "v2_0_0.sql", "-- reshape")
    ];

    void Because() => _pending = MigrationPlan.Pending(
        _available,
        new HashSet<MigrationVersion> { new(1, 0, 0) });

    [Fact] void should_skip_the_applied_migration() =>
        _pending.Any(migration => migration.Version == new MigrationVersion(1, 0, 0)).ShouldBeFalse();
    [Fact] void should_return_only_the_pending_migrations() => _pending.Count.ShouldEqual(2);
    [Fact] void should_order_the_pending_migrations_ascending() =>
        string.Join(',', _pending.Select(migration => migration.Version.ToString())).ShouldEqual("1.1.0,2.0.0");
}
