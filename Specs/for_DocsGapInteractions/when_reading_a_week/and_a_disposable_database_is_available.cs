// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Prompter.Digest;
using Cratis.Prompter.Storage;
using Npgsql;
using NpgsqlTypes;

namespace Cratis.Prompter.Specs.for_DocsGapInteractions.when_reading_a_week;

public class and_a_disposable_database_is_available : Specification
{
    readonly string _schema = $"digest_{Guid.NewGuid():N}";
    readonly DateTimeOffset _from = new(2026, 11, 2, 8, 0, 0, TimeSpan.Zero);
    NpgsqlDataSource _database = null!;
    IReadOnlyList<DocsGapRow> _rows = [];

    public static bool HasDatabase => int.TryParse(Environment.GetEnvironmentVariable("PROMPTER_LEDGER_TEST_PORT"), out _);

    static void Check(bool result)
    {
        Assert.SkipUnless(HasDatabase, "Requires an isolated Postgres port and Lane A migrations after fold.");
        result.ShouldBeTrue();
    }

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
            SearchPath = _schema
        };
        _database = NpgsqlDataSource.Create(settings);
        await Execute($"CREATE SCHEMA {_schema}");
        foreach (var migration in MigrationPlan.Pending(EmbeddedMigrations.LoadFrom(typeof(DocsGapInteractions).Assembly), new HashSet<MigrationVersion>()))
        {
            await Execute(migration.Sql);
        }

        await Insert(_from, (2, 2, false, null));
        await Insert(_from.AddDays(1), (3, 1, false, null));
        await Insert(_from.AddDays(2), (4, 2, false, null));
        await Insert(_from.AddDays(3), (1, 2, false, "down"));
        await Insert(_from.AddDays(4), (0, 0, true, null));
        await Insert(_from.AddDays(5), (1, 2, false, null));
        await Insert(_from.AddDays(6), (5, 0, false, null));
        await Insert(_from.AddTicks(-10), (2, 2, false, null));
        await Insert(_from.AddDays(7), (2, 2, false, null));
    }

    async Task Because()
    {
        if (HasDatabase)
        {
            _rows = await new DocsGapInteractions(_database).Read(_from, _from.AddDays(7), CancellationToken.None);
        }
    }

    async Task Destroy()
    {
        if (_database is not null)
        {
            await Execute($"DROP SCHEMA {_schema} CASCADE");
            await _database.DisposeAsync();
        }
    }

    async Task Insert(DateTimeOffset at, (short Category, short Product, bool Refusal, string? Feedback) row)
    {
        await using var command = _database.CreateCommand("INSERT INTO interactions (occurred_at, source, cited_pages, confidence, was_refusal, feedback, category, product) VALUES ($1, 'spec', ARRAY['https://cratis.io/arc/events.md'], 0.5, $2, $3, $4, $5)");
        command.Parameters.AddWithValue(at);
        command.Parameters.AddWithValue(row.Refusal);
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Text, Value = (object?)row.Feedback ?? DBNull.Value });
        command.Parameters.AddWithValue(row.Category);
        command.Parameters.AddWithValue(row.Product);

        await command.ExecuteNonQueryAsync();
    }

    async Task Execute(string sql)
    {
        await using var command = _database.CreateCommand(sql);
        await command.ExecuteNonQueryAsync();
    }

    [Fact] void should_filter_candidates_and_use_a_half_open_window() => Check(_rows.Count == 5);
    [Fact] void should_map_metadata() => Check(_rows.Any(row => row.Source == "spec" && row.CitedPages.Count == 1 && row.CitedPages[0] == "https://cratis.io/arc/events.md" && row.Category == "answered" && row.Product == "arc" && row.Feedback == "down" && !row.WasRefusal));
    [Fact] void should_map_legacy_unknown_codes_to_null() => Check(_rows.Any(row => row.WasRefusal && row.Category is null && row.Product is null && row.Feedback is null));
}
