// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace Cratis.Prompter.Protection;

/// <summary>Stores attempts and funding atomically in the authoritative Postgres ledger.</summary>
/// <param name="database">The shared database.</param>
/// <param name="options">Shared and provider daily ceilings.</param>
public sealed class ProviderAttemptStore(NpgsqlDataSource database, IOptions<PrompterOptions> options) : IProviderAttemptStore
{
    /// <inheritdoc/>
    public async Task<bool> Reserve(ProviderDispatch dispatch, Guid? parent, decimal upper, CancellationToken cancellationToken)
    {
        var limits = options.Value.Protection;
        await using var command = database.CreateCommand("SELECT reserve_prompter_attempt($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11,$12)");
        Metadata(command, dispatch);
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Uuid, Value = parent switch { { } value => value, _ => DBNull.Value } });
        command.Parameters.AddWithValue(upper);
        command.Parameters.AddWithValue(limits.DailyUsd);
        command.Parameters.AddWithValue(limits.Anthropic.DailyUsd);
        command.Parameters.AddWithValue(limits.Voyage.DailyUsd);
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    /// <inheritdoc/>
    public async Task<bool> Complete(ProviderDispatch dispatch, decimal? cost, CancellationToken cancellationToken)
    {
        await using var command = database.CreateCommand("SELECT complete_prompter_attempt($1,$2,$3,$4,$5,$6,$7,$8)");
        Metadata(command, dispatch);
        command.Parameters.Add(new NpgsqlParameter { NpgsqlDbType = NpgsqlDbType.Numeric, Value = cost switch { { } value => value, _ => DBNull.Value } });
        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    static void Metadata(NpgsqlCommand command, ProviderDispatch dispatch)
    {
        command.Parameters.AddWithValue(dispatch.OperationId);
        command.Parameters.AddWithValue(dispatch.Provider);
        command.Parameters.AddWithValue(dispatch.Account);
        command.Parameters.AddWithValue(dispatch.Task);
        command.Parameters.AddWithValue(dispatch.Attempt);
        command.Parameters.AddWithValue(dispatch.Model);
        command.Parameters.AddWithValue(dispatch.Effort);
    }
}
