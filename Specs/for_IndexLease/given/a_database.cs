// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;
using Npgsql;

namespace Cratis.Prompter.Specs.for_IndexLease.given;

public class a_database : Specification
{
    protected NpgsqlDataSource Database { get; private set; } = null!;

    async Task Establish()
    {
        Database = NpgsqlDataSource.Create("Host=127.0.0.1;Port=55439;Database=postgres;Username=postgres;Pooling=false");
        await using var setup = Database.CreateCommand(
            """
            DROP TABLE IF EXISTS index_reservations;
            DROP TABLE IF EXISTS index_profile;
            DROP TABLE IF EXISTS chunks;
            CREATE TABLE chunks (id TEXT PRIMARY KEY);
            INSERT INTO chunks VALUES ('keep'), ('stale');
            """);
        await setup.ExecuteNonQueryAsync();
        var migration = EmbeddedMigrations.LoadFrom(typeof(Chunks).Assembly).Single(item => item.ResourceName.EndsWith("v1_3_0.sql", StringComparison.Ordinal));
        await using var migrate = Database.CreateCommand(migration.Sql);
        await migrate.ExecuteNonQueryAsync();
    }

    async Task Destroy() => await Database.DisposeAsync();
}
