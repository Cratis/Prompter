// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Npgsql;

namespace Cratis.Prompter.Telemetry;

/// <summary>Best-effort bounded writes. Telemetry outages never hide or change indexing results.</summary>
/// <param name="dataSource">The shared aggregate database.</param>
public sealed class IndexHistory(NpgsqlDataSource dataSource) : IIndexHistory
{
    /// <inheritdoc/>
    public Task Attempt(CancellationToken cancellationToken) => Write(
        "UPDATE prompter_index_health SET attempts = attempts + 1, last_attempt = clock_timestamp() WHERE singleton", null, cancellationToken);

    /// <inheritdoc/>
    public Task Complete(string outcome, IndexRun? run = null, double seconds = 0) => Write(
        """
        UPDATE prompter_index_health SET
            successes = successes + CASE WHEN @outcome = 'success' THEN 1 ELSE 0 END,
            failures = failures + CASE WHEN @outcome = 'failed' THEN 1 ELSE 0 END,
            cancellations = cancellations + CASE WHEN @outcome = 'cancelled' THEN 1 ELSE 0 END,
            last_success = CASE WHEN @outcome = 'success' THEN clock_timestamp() ELSE last_success END,
            last_failure = CASE WHEN @outcome <> 'success' THEN clock_timestamp() ELSE last_failure END,
            duration_seconds = @seconds,
            documents = CASE WHEN @outcome = 'success' THEN @documents ELSE documents END,
            chunks = CASE WHEN @outcome = 'success' THEN @chunks ELSE chunks END
        WHERE singleton
        """,
        command =>
        {
            command.Parameters.AddWithValue(nameof(outcome), outcome);
            command.Parameters.AddWithValue(nameof(seconds), seconds);
            command.Parameters.AddWithValue("documents", run?.Pages ?? 0);
            command.Parameters.AddWithValue("chunks", (run?.Embedded ?? 0) + (run?.Unchanged ?? 0));
        },
        CancellationToken.None);

    async Task Write(string sql, Action<NpgsqlCommand>? configure, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            await using var command = dataSource.CreateCommand(sql);
            configure?.Invoke(command);
            await command.ExecuteNonQueryAsync(timeout.Token);
        }
        catch (Exception error) when (error is NpgsqlException or OperationCanceledException or TimeoutException)
        {
            ProviderMetrics.Event("index", "telemetry_failure");
        }
    }
}
