// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;
using Npgsql;

namespace Cratis.Prompter.Specs.for_IssueDeliveries.when_claiming;

public class and_an_isolated_database_is_supplied : Specification
{
    readonly string _schema = $"deliveries_{Guid.NewGuid():N}";
    NpgsqlDataSource _database = null!;
    bool _first;
    bool _repeat;
    bool _different;
    int _winners;
    Exception? _cancellation;

    public static bool HasDatabase => int.TryParse(Environment.GetEnvironmentVariable("PROMPTER_LEDGER_TEST_PORT"), out _);

    async Task Establish()
    {
        if (!HasDatabase) return;
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
        await using var migration = typeof(IssueDeliveries).Assembly.GetManifestResourceStream("Cratis.Prompter.Storage.Migrations.v1_10_0.sql")!;
        using var reader = new StreamReader(migration);
        await Execute(await reader.ReadToEndAsync());
    }

    async Task Because()
    {
        if (!HasDatabase) return;
        var deliveries = new IssueDeliveries(_database);
        _first = await deliveries.Claim("Cratis/Arc", 1, default);
        _repeat = await deliveries.Claim("cratis/arc", 1, default);
        _different = await deliveries.Claim("Cratis/Arc", 2, default);
        var claims = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => deliveries.Claim("Cratis/Arc", 3, default)));
        _winners = claims.Count(won => won);
        _cancellation = await Catch.Exception(() => deliveries.Claim("Cratis/Arc", 4, new CancellationToken(true)));
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_claim_each_normalized_issue_once_with_one_concurrent_winner()
    {
        Assert.SkipUnless(HasDatabase, "Requires an isolated local Postgres port in PROMPTER_LEDGER_TEST_PORT.");
        _first.ShouldBeTrue();
        _repeat.ShouldBeFalse();
        _different.ShouldBeTrue();
        _winners.ShouldEqual(1);
        (_cancellation is OperationCanceledException).ShouldBeTrue();
    }

    async Task Destroy()
    {
        if (_database is null) return;
        await Execute($"DROP SCHEMA {_schema} CASCADE");
        await _database.DisposeAsync();
    }

    async Task Execute(string sql)
    {
        await using var command = _database.CreateCommand(sql);
        await command.ExecuteNonQueryAsync();
    }
}
