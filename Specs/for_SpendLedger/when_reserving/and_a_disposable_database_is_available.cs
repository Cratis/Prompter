// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Cratis.Prompter.Specs.for_SpendLedger.when_reserving;

public class and_a_disposable_database_is_available : Specification
{
    readonly string _schema = $"spend_{Guid.NewGuid():N}";
    NpgsqlDataSource _database = null!;
    SpendLedger _ledger = null!;
    PrompterOptions _options = null!;
    (int Accepted, bool RestartDenied, bool DoubleRefundDenied, bool ProviderDenied, decimal PriorDay, bool TodayUnchanged) _result;

    /// <summary>Gets a value indicating whether the isolated local database was explicitly supplied.</summary>
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
            Port = int.Parse(Environment.GetEnvironmentVariable("PROMPTER_LEDGER_TEST_PORT")!, System.Globalization.CultureInfo.InvariantCulture),
            Username = "postgres",
            Database = "postgres",
            SearchPath = _schema
        };
        _database = NpgsqlDataSource.Create(settings);
        await Execute($"CREATE SCHEMA {_schema}");
        await using var migration = typeof(SpendLedger).Assembly.GetManifestResourceStream("Cratis.Prompter.Storage.Migrations.v1_3_0.sql")!;
        using var reader = new StreamReader(migration);
        await Execute(await reader.ReadToEndAsync());
        _options = new();
        _options.Protection.Anthropic.DailyUsd = 2.5m;
        _options.Protection.Voyage.DailyUsd = 2.5m;
        _ledger = new(_database, Options.Create(_options));
    }

    async Task Because()
    {
        if (!HasDatabase)
        {
            return;
        }

        var claims = await Task.WhenAll(Enumerable.Range(0, 40).Select(_ => _ledger.Reserve(new(0.1m, 0.1m))));
        var accepted = claims.Where(id => id.HasValue).Select(id => id!.Value).ToArray();

        // A fresh connection pool simulates a new process: unsettled reservations still occupy the day.
        await using var restartedDatabase = NpgsqlDataSource.Create(_database.ConnectionString);
        var restarted = new SpendLedger(restartedDatabase, Options.Create(_options));
        var restartDenied = await restarted.Reserve(new(0.1m, 0.1m)) is null;
        await restarted.Reconcile(accepted[0], new(0.01m, 0.01m));
        await restarted.Reconcile(accepted[0], new(0, 0));
        var doubleRefundDenied = await restarted.Reserve(new(0.1m, 0.1m)) is null;
        var providerDenied = await restarted.Reserve(new(0.1m, 0)) is null;
        var before = await Today();
        var prior = Guid.NewGuid();
        await Execute($"INSERT INTO prompter_spend_days VALUES ((clock_timestamp() AT TIME ZONE 'UTC')::date - 1, 0.4, 0.6); INSERT INTO prompter_spend_reservations VALUES ('{prior}', (clock_timestamp() AT TIME ZONE 'UTC')::date - 1, 0.4, 0.6, false)");
        await restarted.Reconcile(prior, new(0.2m, 0.1m));
        await using var previous = _database.CreateCommand("SELECT anthropic + voyage FROM prompter_spend_days WHERE day = (clock_timestamp() AT TIME ZONE 'UTC')::date - 1");
        var priorTotal = (decimal)(await previous.ExecuteScalarAsync())!;
        _result = (accepted.Length, restartDenied, doubleRefundDenied, providerDenied, priorTotal, before == await Today());
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_atomically_preserve_global_provider_restart_and_utc_day_boundaries()
    {
        Assert.SkipUnless(HasDatabase, "Requires an isolated local Postgres port in PROMPTER_LEDGER_TEST_PORT.");
        _result.ShouldEqual((25, true, true, true, 0.3m, true));
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

    async Task<decimal> Today()
    {
        await using var command = _database.CreateCommand("SELECT anthropic + voyage FROM prompter_spend_days WHERE day = (clock_timestamp() AT TIME ZONE 'UTC')::date");
        return (decimal)(await command.ExecuteScalarAsync())!;
    }
}
