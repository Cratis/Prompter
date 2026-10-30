// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Cratis.Prompter.Specs.for_ProviderAttemptStore.when_funding;

public class and_a_disposable_database_is_available : Specification
{
    readonly string _schema = $"attempt_{Guid.NewGuid():N}";
    NpgsqlDataSource _database = null!;
    IOptions<PrompterOptions> _options = null!;
    ProviderAttemptStore _store = null!;
    SpendLedger _ledger = null!;
    bool _prepaid;
    bool _retry;
    bool _duplicate;
    bool _refund;
    bool _embeddingEnvelope;
    bool _priorDay;
    bool _restart;
    int _concurrent;

    /// <summary>Gets whether CI or an explicitly supplied isolated database is available.</summary>
    public static bool HasDatabase => int.TryParse(Environment.GetEnvironmentVariable("PROMPTER_LEDGER_TEST_PORT"), out _);

    static void Check(bool result)
    {
        Assert.SkipUnless(HasDatabase, "Requires an isolated Postgres port in PROMPTER_LEDGER_TEST_PORT.");
        result.ShouldBeTrue();
    }

    static ProviderDispatch Dispatch() => new(Guid.NewGuid(), "anthropic", "primary", "answer", 1, 100, 100, "claude-sonnet-5-5", string.Empty);

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
        await Migrate("v1_3_0");
        await Migrate("v1_6_0");
        var config = new PrompterOptions();
        config.Protection.DailyUsd = 1;
        config.Protection.Anthropic.DailyUsd = 1;
        config.Protection.Voyage.DailyUsd = 1;
        _options = Options.Create(config);
        _store = new(_database, _options);
        _ledger = new(_database, _options);
    }

    async Task Because()
    {
        if (!HasDatabase)
        {
            return;
        }

        var parent = await _ledger.Reserve(new(0.4m, 0.2m));
        var first = Dispatch();
        var retry = first with { Attempt = 2 };
        _prepaid = await Reserve(first, parent, 0.4m) && await Today() == 0.6m;
        _retry = await Reserve(retry, null, 0.4m) && await Today() == 1m;
        _duplicate = !await Reserve(retry with { Account = "secondary" }, null, 0.4m);
        await _store.Complete(retry, 0.1m, CancellationToken.None);
        _refund = !await _store.Complete(retry, 0, CancellationToken.None) && await Today() == 0.7m;
        await _store.Complete(first, 0.1m, CancellationToken.None);
        _prepaid &= await Today() == 0.7m;
        var embedding = Dispatch() with { Provider = "voyage", Task = "query", Model = "voyage-4" };
        _embeddingEnvelope = await Reserve(embedding, parent, 0.1m) && await Reserve(embedding with { Attempt = 2 }, parent, 0.1m);
        _embeddingEnvelope &= !await Reserve(embedding with { Attempt = 3 }, parent, 0.1m);
        await _ledger.Reconcile(parent!.Value, new(0.1m, 0.1m));
        await CheckPriorDay();
        await CheckRestart(first);
        var races = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => Reserve(Dispatch(), null, 0.1m)));
        _concurrent = races.Count(accepted => accepted);
    }

    [Fact, Trait("Category", "Integration")] void should_claim_without_double_charging_the_parent() => Check(_prepaid);
    [Fact, Trait("Category", "Integration")] void should_fund_a_retry_under_the_same_global_day_cap() => Check(_retry);
    [Fact, Trait("Category", "Integration")] void should_deny_duplicate_and_cross_account_replay() => Check(_duplicate);
    [Fact, Trait("Category", "Integration")] void should_settle_independent_funding_at_most_once() => Check(_refund);
    [Fact, Trait("Category", "Integration")] void should_bound_embedding_claims_by_the_parent_envelope() => Check(_embeddingEnvelope);
    [Fact, Trait("Category", "Integration")] void should_settle_the_original_utc_day_only() => Check(_priorDay);
    [Fact, Trait("Category", "Integration")] void should_retain_unknown_usage_across_connection_pools() => Check(_restart);
    [Fact, Trait("Category", "Integration")] void should_atomically_admit_only_funded_concurrent_attempts() => Check(_concurrent == 6);

    async Task CheckPriorDay()
    {
        var parent = Guid.NewGuid();
        await Execute($"INSERT INTO prompter_spend_days VALUES ((clock_timestamp() AT TIME ZONE 'UTC')::date - 1, 0.4, 0); INSERT INTO prompter_spend_reservations VALUES ('{parent}', (clock_timestamp() AT TIME ZONE 'UTC')::date - 1, 0.4, 0, false)");
        var before = await Today();
        _priorDay = await Reserve(Dispatch(), parent, 0.4m);
        await _ledger.Reconcile(parent, new(0.1m, 0));
        await using var command = _database.CreateCommand("SELECT anthropic FROM prompter_spend_days WHERE day = (clock_timestamp() AT TIME ZONE 'UTC')::date - 1");
        _priorDay &= (decimal)(await command.ExecuteScalarAsync())! == 0.1m && await Today() == before;
    }

    async Task CheckRestart(ProviderDispatch first)
    {
        await using var restarted = NpgsqlDataSource.Create(_database.ConnectionString);
        var store = new ProviderAttemptStore(restarted, _options);
        var unknown = Dispatch();
        _restart = !await store.Reserve(first, null, 0.1m, CancellationToken.None);
        _restart &= await store.Reserve(unknown, null, 0.1m, CancellationToken.None);
        _restart &= await store.Complete(unknown, null, CancellationToken.None);
        _restart &= !await store.Complete(unknown, 0, CancellationToken.None) && await Today() == 0.4m;
    }

    Task<bool> Reserve(ProviderDispatch dispatch, Guid? parent, decimal upper) => _store.Reserve(dispatch, parent, upper, CancellationToken.None);

    async Task Migrate(string version)
    {
        await using var migration = typeof(SpendLedger).Assembly.GetManifestResourceStream($"Cratis.Prompter.Storage.Migrations.{version}.sql")!;
        using var reader = new StreamReader(migration);
        await Execute(await reader.ReadToEndAsync());
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

    async Task Destroy()
    {
        if (_database is not null)
        {
            await Execute($"DROP SCHEMA {_schema} CASCADE");
            await _database.DisposeAsync();
        }
    }
}
