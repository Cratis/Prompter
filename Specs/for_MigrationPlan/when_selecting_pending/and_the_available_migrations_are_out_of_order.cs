// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_MigrationPlan.when_selecting_pending;

public class and_the_available_migrations_are_out_of_order : Specification
{
    Migration[] _available = null!;
    IReadOnlyList<Migration> _pending = null!;

    void Establish() => _available =
    [
        new Migration(new MigrationVersion(2, 0, 0), "v2_0_0.sql", "-- reshape"),
        new Migration(new MigrationVersion(1, 0, 0), "v1_0_0.sql", "-- baseline"),
        new Migration(new MigrationVersion(1, 10, 0), "v1_10_0.sql", "-- later than 1.9"),
        new Migration(new MigrationVersion(1, 9, 0), "v1_9_0.sql", "-- earlier than 1.10")
    ];

    void Because() => _pending = MigrationPlan.Pending(_available, new HashSet<MigrationVersion>());

    [Fact] void should_order_by_semantic_version_not_lexically() =>
        string.Join(',', _pending.Select(migration => migration.Version.ToString()))
            .ShouldEqual("1.0.0,1.9.0,1.10.0,2.0.0");
}
