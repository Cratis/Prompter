// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Prompter.Storage;
using Npgsql;

namespace Cratis.Prompter.Specs.for_AnsweringPauses.when_setting;

public class and_a_disposable_database_is_available : Specification
{
    readonly string _schema = $"pauses_{Guid.NewGuid():N}";
    NpgsqlDataSource _database = null!;
    bool _initial;
    bool _paused;
    bool _resumed;

    public static bool HasDatabase => int.TryParse(Environment.GetEnvironmentVariable("PROMPTER_LEDGER_TEST_PORT"), out _);

    async Task Establish()
    {
        if (!HasDatabase)
        {
            return;
        }

        var settings = new NpgsqlConnectionStringBuilder
        {
            Host = "127.0.0.1",
            Port = int.Parse(Environment.GetEnvironmentVariable("PROMPTER_LEDGER_TEST_PORT")!, CultureInfo.InvariantCulture),
            Username = "postgres",
            Database = "postgres",
            SearchPath = $"{_schema},public"
        };
        _database = NpgsqlDataSource.Create(settings);

        // The migrations use the pgvector type, which lives in the public schema shared by every fixture.
        await VectorExtension.Ensure(_database);

        await Execute($"CREATE SCHEMA {_schema}");
        foreach (var migration in MigrationPlan.Pending(EmbeddedMigrations.LoadFrom(typeof(AnsweringPauses).Assembly), new HashSet<MigrationVersion>()))
        {
            await Execute(migration.Sql);
        }
    }

    async Task Because()
    {
        if (!HasDatabase)
        {
            return;
        }

        var pauses = new AnsweringPauses(_database);
        _initial = await pauses.IsPaused(default);
        await pauses.Set(true, default);
        _paused = await pauses.IsPaused(default);
        await pauses.Set(false, default);
        _resumed = await pauses.IsPaused(default);
    }

    async Task Destroy()
    {
        if (_database is not null)
        {
            await Execute($"DROP SCHEMA {_schema} CASCADE");
            await _database.DisposeAsync();
        }
    }

    async Task Execute(string sql)
    {
        await using var command = _database.CreateCommand(sql);
        await command.ExecuteNonQueryAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_default_to_not_paused()
    {
        Assert.SkipUnless(HasDatabase, "Requires an isolated local Postgres port in PROMPTER_LEDGER_TEST_PORT.");
        _initial.ShouldBeFalse();
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_read_back_a_pause()
    {
        Assert.SkipUnless(HasDatabase, "Requires an isolated local Postgres port in PROMPTER_LEDGER_TEST_PORT.");
        _paused.ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_read_back_a_resume()
    {
        Assert.SkipUnless(HasDatabase, "Requires an isolated local Postgres port in PROMPTER_LEDGER_TEST_PORT.");
        _resumed.ShouldBeFalse();
    }
}
