// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Specs.for_ApplicationRole.given;
using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_Chunks.when_ensuring_schema;

public class and_the_role_may_not_migrate : a_provisioned_application_role
{
    Exception? _result;

    async Task Because()
    {
        if (!HasDatabase) return;
        var highest = EmbeddedMigrations.LoadFrom(typeof(Chunks).Assembly)
            .OrderBy(migration => (migration.Version.Major, migration.Version.Minor, migration.Version.Patch))
            .Last();
        await Execute($"DELETE FROM {Schema}.schema_migrations WHERE version = '{highest.Version}'");
        _result = await Catch.Exception(() => new Chunks(App).EnsureSchema());
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_report_the_pending_migrations_as_not_permitted()
    {
        Assert.SkipUnless(HasDatabase, "Requires an isolated local Postgres port in PROMPTER_LEDGER_TEST_PORT.");
        (_result is PendingMigrationsNotPermitted).ShouldBeTrue();
    }
}
