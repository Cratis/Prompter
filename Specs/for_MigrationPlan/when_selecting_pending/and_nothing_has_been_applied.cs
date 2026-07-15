// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_MigrationPlan.when_selecting_pending;

public class and_nothing_has_been_applied : Specification
{
    Migration[] _available = null!;
    IReadOnlyList<Migration> _pending = null!;

    void Establish() => _available =
    [
        new Migration(new MigrationVersion(1, 0, 0), "v1_0_0.sql", "-- baseline"),
        new Migration(new MigrationVersion(1, 1, 0), "v1_1_0.sql", "-- feedback")
    ];

    void Because() => _pending = MigrationPlan.Pending(_available, new HashSet<MigrationVersion>());

    [Fact] void should_return_every_migration() => _pending.Count.ShouldEqual(2);
    [Fact] void should_order_them_ascending() =>
        string.Join(',', _pending.Select(migration => migration.Version.ToString())).ShouldEqual("1.0.0,1.1.0");
}
