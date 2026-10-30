// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Npgsql;
using Pgvector;

namespace Cratis.Prompter.Storage;

/// <summary>Builds identical chunk writes for direct and atomic publication.</summary>
internal static class ChunkCommands
{
    /// <summary>Populates a parameterized chunk upsert.</summary>
    /// <param name="command">The command to populate.</param>
    /// <param name="chunk">The source chunk.</param>
    /// <param name="embedding">The generated embedding.</param>
    internal static void PopulateUpsert(NpgsqlCommand command, Chunk chunk, ReadOnlyMemory<float> embedding)
    {
        command.CommandText =
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
            """;
        command.Parameters.AddWithValue(chunk.Id.Value);
        command.Parameters.AddWithValue(chunk.Page.Value);
        command.Parameters.AddWithValue(chunk.Title);
        command.Parameters.AddWithValue(chunk.HeadingPath);
        command.Parameters.AddWithValue(chunk.Content);
        command.Parameters.AddWithValue(chunk.Hash.Value);
        command.Parameters.AddWithValue(new Vector(embedding));
    }
}
