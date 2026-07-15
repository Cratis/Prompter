// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;
using Npgsql;

namespace Cratis.Prompter.Storage;

/// <summary>
/// Represents an implementation of <see cref="IInteractionLog"/> backed by Postgres.
/// </summary>
/// <param name="dataSource">The Postgres data source.</param>
/// <param name="options">The Prompter options.</param>
public class InteractionLog(NpgsqlDataSource dataSource, IOptions<PrompterOptions> options) : IInteractionLog
{
    /// <inheritdoc/>
    public async Task Record(Interaction interaction, CancellationToken cancellationToken = default)
    {
        await using var command = dataSource.CreateCommand(
            """
            INSERT INTO interactions (user_hash, source, question, answer, cited_pages, confidence, was_refusal)
            VALUES ($1, $2, $3, $4, $5, $6, $7)
            """);

        command.Parameters.AddWithValue(interaction.UserHash);
        command.Parameters.AddWithValue(interaction.Source);
        command.Parameters.AddWithValue(interaction.Question);
        command.Parameters.AddWithValue(interaction.Answer);
        command.Parameters.AddWithValue(interaction.CitedPages.Select(page => page.Value).ToArray());
        command.Parameters.AddWithValue(interaction.Confidence);
        command.Parameters.AddWithValue(interaction.WasRefusal);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> PurgeExpired(CancellationToken cancellationToken = default)
    {
        await using var command = dataSource.CreateCommand(
            "DELETE FROM interactions WHERE occurred_at < now() - make_interval(days => $1)");
        command.Parameters.AddWithValue(options.Value.RetentionDays);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
