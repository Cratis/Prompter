// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Npgsql;

namespace Cratis.Prompter.Specs;

/// <summary>
/// Creates the pgvector extension once for every database-backed specification sharing a disposable server.
/// </summary>
public static class VectorExtension
{
    static readonly SemaphoreSlim _creation = new(1, 1);

    /// <summary>
    /// Ensures the pgvector extension exists in the public schema of the database behind <paramref name="database"/>.
    /// </summary>
    /// <param name="database">A data source connected as a role allowed to create the extension.</param>
    /// <returns>A task that completes when the extension exists.</returns>
    public static async Task Ensure(NpgsqlDataSource database)
    {
        await _creation.WaitAsync();
        try
        {
            await using var command = database.CreateCommand("CREATE EXTENSION IF NOT EXISTS vector WITH SCHEMA public");
            await command.ExecuteNonQueryAsync();
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            // Another process created the extension between the existence check and the insert; it exists now.
        }
        finally
        {
            _creation.Release();
        }
    }
}
