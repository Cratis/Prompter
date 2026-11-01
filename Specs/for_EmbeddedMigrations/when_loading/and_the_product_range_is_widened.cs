// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_EmbeddedMigrations.when_loading;

public class and_the_product_range_is_widened : Specification
{
    Migration _migration = null!;
    void Because() => _migration = EmbeddedMigrations.LoadFrom(typeof(Chunks).Assembly).Single(migration => migration.Version == new MigrationVersion(1, 8, 0));
    [Fact] void should_widen_without_renumbering_existing_rows() => _migration.Sql.ShouldContain("CHECK (product BETWEEN 0 AND 12)");
    [Fact] void should_be_repeatable() => _migration.Sql.ShouldContain("DROP CONSTRAINT IF EXISTS interactions_product_check");
}
