// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_MigrationPlan.when_selecting_pending;

public class and_every_version_is_applied : Specification
{
    Migration[] _available = null!;
    IReadOnlyList<Migration> _pending = null!;

    void Establish() => _available =
    [
        new Migration(new MigrationVersion(1, 0, 0), "v1_0_0.sql", "-- baseline"),
        new Migration(new MigrationVersion(1, 1, 0), "v1_1_0.sql", "-- feedback")
    ];

    void Because() => _pending = MigrationPlan.Pending(
        _available,
        new HashSet<MigrationVersion> { new(1, 0, 0), new(1, 1, 0) });

    [Fact] void should_return_nothing() => _pending.ShouldBeEmpty();
}
