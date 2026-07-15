// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
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
    const string SchemaResourceName = "Cratis.Prompter.Storage.Schema.sql";

    /// <inheritdoc/>
    public async Task EnsureSchema(CancellationToken cancellationToken = default)
    {
        await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(SchemaResourceName)
            ?? throw new MissingSchemaResource(SchemaResourceName);
        using var reader = new StreamReader(stream);
        var schema = await reader.ReadToEndAsync(cancellationToken);

        await using var command = dataSource.CreateCommand(schema);
        await command.ExecuteNonQueryAsync(cancellationToken);
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
}
