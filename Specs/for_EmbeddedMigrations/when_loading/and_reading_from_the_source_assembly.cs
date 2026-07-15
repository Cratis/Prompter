// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_EmbeddedMigrations.when_loading;

public class and_reading_from_the_source_assembly : Specification
{
    IReadOnlyList<Migration> _migrations = null!;
    Migration _baseline = null!;

    void Because()
    {
        _migrations = EmbeddedMigrations.LoadFrom(typeof(Chunks).Assembly);
        _baseline = _migrations.Single(migration => migration.Version == new MigrationVersion(1, 0, 0));
    }

    [Fact] void should_discover_the_baseline_migration() => _baseline.ShouldNotBeNull();
    [Fact] void should_carry_the_manifest_resource_name() =>
        _baseline.ResourceName.ShouldEqual("Cratis.Prompter.Storage.Migrations.v1_0_0.sql");
    [Fact] void should_include_the_chunks_table() => _baseline.Sql.ShouldContain("CREATE TABLE IF NOT EXISTS chunks");
    [Fact] void should_include_the_interactions_table() => _baseline.Sql.ShouldContain("CREATE TABLE IF NOT EXISTS interactions");
}
