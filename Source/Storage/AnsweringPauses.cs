// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Npgsql;

namespace Cratis.Prompter.Storage;

/// <summary>Stores the pause flag in a single row. No actor identity is stored.</summary>
/// <param name="database">The shared database connection pool.</param>
public sealed class AnsweringPauses(NpgsqlDataSource database) : IAnsweringPauses
{
    /// <inheritdoc/>
    public async Task<bool> IsPaused(CancellationToken cancellationToken)
    {
        await using var command = database.CreateCommand("SELECT paused FROM answering_pause");
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    /// <inheritdoc/>
    public async Task Set(bool paused, CancellationToken cancellationToken)
    {
        await using var command = database.CreateCommand("INSERT INTO answering_pause (singleton, paused) VALUES (TRUE, $1) ON CONFLICT (singleton) DO UPDATE SET paused = EXCLUDED.paused, changed_at = now()");
        command.Parameters.AddWithValue(paused);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
