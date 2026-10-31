// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Npgsql;

namespace Cratis.Prompter.Specs;

/// <summary>
/// Installs the pgvector extension into the shared "public" schema, serialized across every disposable-database
/// spec in this process. Postgres does not make "CREATE EXTENSION IF NOT EXISTS" safe against two sessions
/// racing to install the same extension for the first time, so every spec that needs the "vector" type goes
/// through this rather than running the statement directly.
/// </summary>
public static class VectorExtension
{
    static readonly SemaphoreSlim _creation = new(1, 1);

    /// <summary>
    /// Ensures the pgvector extension is installed in the "public" schema of the given database.
    /// </summary>
    /// <param name="database">The database to install the extension into.</param>
    public static async Task EnsureInstalled(NpgsqlDataSource database)
    {
        await _creation.WaitAsync();
        try
        {
            await using var command = database.CreateCommand("CREATE EXTENSION IF NOT EXISTS vector WITH SCHEMA public");
            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            _creation.Release();
        }
    }
}
