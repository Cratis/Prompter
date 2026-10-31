// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Prompter.Answering;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Cratis.Prompter.Specs.for_InteractionLog.when_recording;

public class a_classified_interaction : Specification
{
    static readonly string[] _migrations = ["v1_0_0", "v1_1_0", "v1_2_0"];
    static readonly SemaphoreSlim _extensionCreation = new(1, 1);
    readonly string _schema = $"interactions_{Guid.NewGuid():N}";
    NpgsqlDataSource? _database;
    long _id;
    (short Category, short Product) _stored;
    (short Category, short Product) _old;
    Exception? _invalid;

    static bool HasDatabase => Environment.GetEnvironmentVariable("PROMPTER_RETRIEVAL_TEST_PORT") is not null;

    static void Require(Action check)
    {
        Assert.SkipUnless(HasDatabase, "Requires disposable pgvector in PROMPTER_RETRIEVAL_TEST_PORT.");
        check();
    }

    static async Task Execute(NpgsqlDataSource database, string sql)
    {
        await using var command = database.CreateCommand(sql);
        await command.ExecuteNonQueryAsync();
    }

    static async Task<string> Migration(string name)
    {
        await using var stream = typeof(Interaction).Assembly.GetManifestResourceStream($"{EmbeddedMigrations.ResourcePrefix}{name}.sql")!;
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    async Task Establish()
    {
        if (!HasDatabase)
        {
            return;
        }

        Assert.True(int.TryParse(Environment.GetEnvironmentVariable("PROMPTER_RETRIEVAL_TEST_PORT"), NumberStyles.None, CultureInfo.InvariantCulture, out var port) && port is > 0 and <= 65535, "Invalid disposable port.");
        var settings = new NpgsqlConnectionStringBuilder { Host = "127.0.0.1", Port = port, Username = "postgres", Database = "postgres", SearchPath = "public" };
        await using (var admin = NpgsqlDataSource.Create(settings))
        {
            await _extensionCreation.WaitAsync();
            try
            {
                await Execute(admin, "CREATE EXTENSION IF NOT EXISTS vector WITH SCHEMA public");
            }
            finally
            {
                _extensionCreation.Release();
            }

            await Execute(admin, $"CREATE SCHEMA {_schema}");
        }

        settings.SearchPath = $"{_schema},public";
        var builder = new NpgsqlDataSourceBuilder(settings.ConnectionString);
        builder.UseVector();
        _database = builder.Build();
        foreach (var migration in _migrations)
        {
            await Execute(_database, await Migration(migration));
        }

        await Execute(_database, "INSERT INTO interactions (source, cited_pages, confidence, was_refusal) VALUES ('cli', ARRAY[]::text[], 0.5, false)");
        await Execute(_database, await Migration("v1_7_0"));
    }

    async Task Because()
    {
        if (_database is null)
        {
            return;
        }

        var log = new InteractionLog(_database, Options.Create(new PrompterOptions()));
        _id = await log.Record(new("cli", [], 0.9, false) { Category = InteractionCategory.MissingCapability, Product = InteractionProduct.Arc }, TestContext.Current.CancellationToken);
        _stored = await Codes($"id = {_id}");
        _old = await Codes("source = 'cli' AND confidence = 0.5");
        _invalid = await Catch.Exception(() => Execute(_database, "INSERT INTO interactions (source, cited_pages, confidence, was_refusal, category) VALUES ('cli', ARRAY[]::text[], 0.1, false, 6)"));
    }

    [Fact] void should_store_the_stable_category_code() => Require(() => _stored.Category.ShouldEqual((short)3));
    [Fact] void should_store_the_stable_product_code() => Require(() => _stored.Product.ShouldEqual((short)2));
    [Fact] void should_keep_old_rows_unknown() => Require(() => _old.ShouldEqual(((short)0, (short)0)));
    [Fact] void should_reject_invalid_codes() => Require(_invalid.ShouldBeOfExactType<PostgresException>);

    async Task<(short Category, short Product)> Codes(string filter)
    {
        await using var command = _database!.CreateCommand($"SELECT category, product FROM interactions WHERE {filter}");
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return (reader.GetInt16(0), reader.GetInt16(1));
    }

    async Task Destroy()
    {
        if (_database is not null)
        {
            await Execute(_database, $"DROP SCHEMA {_schema} CASCADE");
            await _database.DisposeAsync();
        }
    }
}
