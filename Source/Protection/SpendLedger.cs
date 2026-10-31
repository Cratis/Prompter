// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;
using Npgsql;

namespace Cratis.Prompter.Protection;

/// <summary>Postgres serializes all reservations for a UTC day, across processes and restarts.</summary>
/// <param name="dataSource">The shared database.</param>
/// <param name="options">The global and provider sublimits.</param>
public sealed class SpendLedger(NpgsqlDataSource dataSource, IOptions<PrompterOptions> options) : ISpendLedger
{
    /// <inheritdoc/>
    public async Task<Guid?> Reserve(SpendExposure exposure, CancellationToken cancellationToken = default)
    {
        if (!exposure.IsValid)
        {
            throw new WorkRefused();
        }

        var limits = options.Value.Protection;
        await using var command = dataSource.CreateCommand("SELECT reserve_prompter_spend($1, $2, $3, $4, $5, $6)");
        var id = Guid.NewGuid();
        command.Parameters.AddWithValue(id);
        command.Parameters.AddWithValue(exposure.Anthropic);
        command.Parameters.AddWithValue(exposure.Voyage);
        command.Parameters.AddWithValue(limits.DailyUsd);
        command.Parameters.AddWithValue(limits.Anthropic.DailyUsd);
        command.Parameters.AddWithValue(limits.Voyage.DailyUsd);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is Guid reserved ? reserved : null;
    }

    /// <inheritdoc/>
    public async Task Reconcile(Guid reservation, SpendExposure exposure, CancellationToken cancellationToken = default)
    {
        await using var command = dataSource.CreateCommand("SELECT reconcile_prompter_spend($1, $2, $3)");
        command.Parameters.AddWithValue(reservation);
        command.Parameters.AddWithValue(exposure.Anthropic);
        command.Parameters.AddWithValue(exposure.Voyage);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
