// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Npgsql;
using Pgvector;

namespace Cratis.Prompter.Storage;

/// <summary>
/// Represents an implementation of <see cref="IChunks"/> backed by Postgres with pgvector.
/// </summary>
/// <param name="dataSource">The Postgres data source.</param>
public class Chunks(NpgsqlDataSource dataSource) : IChunks
{
    const string CreateMigrationsTableSql =
        """
        CREATE TABLE IF NOT EXISTS schema_migrations (
            version TEXT PRIMARY KEY,
            applied_at TIMESTAMPTZ NOT NULL DEFAULT now()
        )
        """;

    /// <inheritdoc/>
    public async Task EnsureSchema(CancellationToken cancellationToken = default)
    {
        await EnsureMigrationsTable(cancellationToken);

        var available = EmbeddedMigrations.LoadFrom(typeof(Chunks).Assembly);
        var applied = await GetAppliedVersions(cancellationToken);

        foreach (var migration in MigrationPlan.Pending(available, applied))
        {
            await Apply(migration, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<ChunkId, ContentHash>> GetHashes(CancellationToken cancellationToken = default)
    {
        var hashes = new Dictionary<ChunkId, ContentHash>();

        await using var command = dataSource.CreateCommand("SELECT id, content_hash FROM chunks");
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            hashes[new ChunkId(reader.GetString(0))] = new ContentHash(reader.GetString(1));
        }

        return hashes;
    }

    /// <inheritdoc/>
    public async Task Upsert(Chunk chunk, ReadOnlyMemory<float> embedding, CancellationToken cancellationToken = default)
    {
        await using var command = dataSource.CreateCommand(
            """
            INSERT INTO chunks (id, page_url, title, heading_path, content, content_hash, embedding)
            VALUES ($1, $2, $3, $4, $5, $6, $7)
            ON CONFLICT (id) DO UPDATE SET
                page_url = EXCLUDED.page_url,
                title = EXCLUDED.title,
                heading_path = EXCLUDED.heading_path,
                content = EXCLUDED.content,
                content_hash = EXCLUDED.content_hash,
                embedding = EXCLUDED.embedding
            """);

        command.Parameters.AddWithValue(chunk.Id.Value);
        command.Parameters.AddWithValue(chunk.Page.Value);
        command.Parameters.AddWithValue(chunk.Title);
        command.Parameters.AddWithValue(chunk.HeadingPath);
        command.Parameters.AddWithValue(chunk.Content);
        command.Parameters.AddWithValue(chunk.Hash.Value);
        command.Parameters.AddWithValue(new Vector(embedding));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> RemoveAllExcept(IReadOnlySet<ChunkId> existing, CancellationToken cancellationToken = default)
    {
        await using var command = dataSource.CreateCommand("DELETE FROM chunks WHERE NOT (id = ANY($1))");
        command.Parameters.AddWithValue(existing.Select(id => id.Value).ToArray());

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    async Task EnsureMigrationsTable(CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(CreateMigrationsTableSql);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    async Task<IReadOnlySet<MigrationVersion>> GetAppliedVersions(CancellationToken cancellationToken)
    {
        var applied = new HashSet<MigrationVersion>();

        await using var command = dataSource.CreateCommand("SELECT version FROM schema_migrations");
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            applied.Add(MigrationVersion.Parse(reader.GetString(0)));
        }

        return applied;
    }

    async Task Apply(Migration migration, CancellationToken cancellationToken)
    {
        // The migration body and its version record run as a single simple-protocol batch inside one
        // explicit transaction: if any statement fails the whole batch rolls back, so a partial run never
        // records a version whose migration did not fully apply. The migration body is multiple statements,
        // which rules out parameters (those force the extended, one-statement-per-command protocol); the
        // version is a validated numeric (major.minor.patch), so embedding it as a literal is injection-safe.
        var sql = $"BEGIN;\n{migration.Sql}\nINSERT INTO schema_migrations (version) VALUES ('{migration.Version}');\nCOMMIT;";

        await using var command = dataSource.CreateCommand(sql);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
