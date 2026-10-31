// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Npgsql;

namespace Cratis.Prompter.Storage;

/// <summary>
/// Owns a Postgres session lock shared by every index entry point and replica.
/// Publication uses this same session so a lost lease cannot publish stale work.
/// </summary>
/// <param name="connection">The connection owning the advisory lock.</param>
public sealed class IndexLease(NpgsqlConnection connection) : IIndexLease
{
    const long LockKey = 0x50524F4D494E44;
    string? _profile;
    string? _storedProfile;

    /// <summary>Acquires without waiting; failure disposes the connection.</summary>
    /// <param name="dataSource">The shared database.</param>
    /// <param name="cancellationToken">Token for cancellation.</param>
    /// <returns>The owning lease.</returns>
    public static async Task<IIndexLease> Acquire(NpgsqlDataSource dataSource, CancellationToken cancellationToken)
    {
        var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT pg_try_advisory_lock($1)";
            command.Parameters.AddWithValue(LockKey);
            if (await command.ExecuteScalarAsync(cancellationToken) is not true) throw new IndexAlreadyRunning();
            return new IndexLease(connection);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task CheckProfile(string profile, bool full, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT profile FROM index_profile WHERE singleton = TRUE";
        var current = await command.ExecuteScalarAsync(cancellationToken);
        if (current is not string stored) throw new IndexProfileMismatch();
        var incompatible = !full && stored != profile;
        if (incompatible) throw new IndexProfileMismatch();
        _profile = profile;
        _storedProfile = stored;
    }

    /// <inheritdoc/>
    public async Task EnsureHeld(CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";
        await command.ExecuteScalarAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task Reserve(IndexExposure exposure, decimal dailyLimit, bool scheduled, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO index_reservations (day, tokens, dollars, scheduled)
            SELECT (now() AT TIME ZONE 'UTC')::date, $1, $2, $3 WHERE $2 = 0 OR $2 <= $4
            ON CONFLICT (day) DO UPDATE SET
                tokens = index_reservations.tokens + EXCLUDED.tokens,
                dollars = index_reservations.dollars + EXCLUDED.dollars,
                scheduled = index_reservations.scheduled OR EXCLUDED.scheduled
            WHERE (EXCLUDED.dollars = 0 OR index_reservations.dollars + EXCLUDED.dollars <= $4)
                AND NOT (index_reservations.scheduled AND EXCLUDED.scheduled)
            """;
        command.Parameters.AddWithValue(exposure.Tokens);
        command.Parameters.AddWithValue(exposure.Dollars);
        command.Parameters.AddWithValue(scheduled);
        command.Parameters.AddWithValue(dailyLimit);
        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1) throw new IndexBudgetExceeded();
    }

    /// <inheritdoc/>
    public Task<int> CommitIndex(IReadOnlyCollection<EmbeddedChunk> changed, IReadOnlySet<ChunkId> existing, IReadOnlyCollection<DocumentationMetadata> pages, CancellationToken cancellationToken) =>
        CommitIndex(new(changed, existing, pages), cancellationToken);

    /// <inheritdoc/>
    public async Task<int> CommitIndex(IndexSnapshot snapshot, CancellationToken cancellationToken)
    {
        if (snapshot.PreservedSources.Count > 0 && _profile != _storedProfile) throw new IndexProfileMismatch();
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        foreach (var item in snapshot.Changed)
        {
            await using var upsert = connection.CreateCommand();
            ChunkCommands.PopulateUpsert(upsert, item.Chunk, item.Vector);
            await upsert.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var remove = connection.CreateCommand();
        remove.CommandText = "DELETE FROM chunks WHERE NOT (id = ANY($1))";
        remove.Parameters.AddWithValue(snapshot.Existing.Select(id => id.Value).ToArray());
        var removed = await remove.ExecuteNonQueryAsync(cancellationToken);
        await PublishPages(snapshot, cancellationToken);
        if (_profile is not null)
        {
            await using var profile = connection.CreateCommand();
            profile.CommandText = "UPDATE index_profile SET profile = $1 WHERE singleton = TRUE";
            profile.Parameters.AddWithValue(_profile);
            await profile.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return removed;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (connection.State == System.Data.ConnectionState.Open)
            {
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT pg_advisory_unlock($1)";
                command.Parameters.AddWithValue(LockKey);
                await command.ExecuteScalarAsync(CancellationToken.None);
            }
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    async Task PublishPages(IndexSnapshot snapshot, CancellationToken cancellationToken)
    {
        // One statement per concern inside the caller's transaction: stamp indexed_at at publication, keep only the snapshot.
        await using var upsert = connection.CreateCommand();
        upsert.CommandText =
            """
            INSERT INTO documentation_pages (page_url, indexed_at, page_last_modified, product_version)
            SELECT url, now(), modified, version FROM unnest($1::text[], $2::timestamptz[], $3::text[]) AS page(url, modified, version)
            ON CONFLICT (page_url) DO UPDATE SET
                indexed_at = EXCLUDED.indexed_at,
                page_last_modified = EXCLUDED.page_last_modified,
                product_version = EXCLUDED.product_version
            """;
        var ordered = snapshot.Pages.ToArray();
        upsert.Parameters.AddWithValue(ordered.Select(page => page.Page.Value).ToArray());
        upsert.Parameters.AddWithValue(ordered.Select(page => page.PageLastModified?.UtcDateTime).ToArray());
        upsert.Parameters.AddWithValue(ordered.Select(page => page.ProductVersion).ToArray());
        await upsert.ExecuteNonQueryAsync(cancellationToken);

        await using var prune = connection.CreateCommand();
        prune.CommandText = "DELETE FROM documentation_pages WHERE NOT (page_url = ANY($1)) AND NOT EXISTS (SELECT 1 FROM unnest($2::text[]) AS retained(prefix) WHERE starts_with(page_url, retained.prefix))";
        prune.Parameters.AddWithValue(ordered.Select(page => page.Page.Value).ToArray());
        prune.Parameters.AddWithValue(snapshot.PreservedSources.ToArray());
        await prune.ExecuteNonQueryAsync(cancellationToken);
    }
}
