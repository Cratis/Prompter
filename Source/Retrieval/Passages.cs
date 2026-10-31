// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Microsoft.Extensions.AI;
using Npgsql;
using Pgvector;

namespace Cratis.Prompter.Retrieval;

/// <summary>
/// Represents an implementation of <see cref="IPassages"/> doing hybrid retrieval in a single
/// Postgres query - full-text and vector ranks fused with reciprocal rank fusion (RRF).
/// </summary>
/// <param name="dataSource">The Postgres data source.</param>
/// <param name="embeddings">The generator used to embed the question.</param>
public class Passages(
    NpgsqlDataSource dataSource,
    IEmbeddingGenerator<string, Embedding<float>> embeddings) : IPassages
{
    const int CandidatesPerMethod = 20;
    const int RrfConstant = 60;

    /// <inheritdoc/>
    public async Task<IEnumerable<Passage>> Search(Question question, int limit, CancellationToken cancellationToken = default)
    {
        var generated = await embeddings.GenerateAsync([question.Value], cancellationToken: cancellationToken);
        var questionEmbedding = new Vector(generated[0].Vector);

        await using var command = dataSource.CreateCommand(
            $"""
            WITH semantic AS (
                SELECT id, RANK() OVER (ORDER BY embedding <=> $1) AS rank
                FROM chunks
                ORDER BY embedding <=> $1
                LIMIT {CandidatesPerMethod}
            ),
            lexical AS (
                SELECT id, RANK() OVER (ORDER BY ts_rank_cd(tsv, websearch_to_tsquery('english', $2)) DESC) AS rank
                FROM chunks
                WHERE tsv @@ websearch_to_tsquery('english', $2)
                ORDER BY ts_rank_cd(tsv, websearch_to_tsquery('english', $2)) DESC
                LIMIT {CandidatesPerMethod}
            )
            SELECT c.page_url, c.title, c.heading_path, c.content,
                   COALESCE(1.0 / ({RrfConstant} + semantic.rank), 0) +
                   COALESCE(1.0 / ({RrfConstant} + lexical.rank), 0) AS score,
                   1.0 - (c.embedding <=> $1) AS semantic_similarity,
                   p.indexed_at, p.page_last_modified, p.product_version
            FROM chunks c
            LEFT JOIN semantic ON c.id = semantic.id
            LEFT JOIN lexical ON c.id = lexical.id
            LEFT JOIN documentation_pages p ON p.page_url = c.page_url
            WHERE semantic.id IS NOT NULL OR lexical.id IS NOT NULL
            ORDER BY score DESC
            LIMIT $3
            """);

        command.Parameters.AddWithValue(questionEmbedding);
        command.Parameters.AddWithValue(question.Value);
        command.Parameters.AddWithValue(limit);

        var passages = new List<Passage>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            passages.Add(await ReadPassage(reader, cancellationToken));
        }

        return passages;
    }

    static async Task<Passage> ReadPassage(NpgsqlDataReader reader, CancellationToken cancellationToken) => new(
        new PageUrl(reader.GetString(0)),
        reader.GetString(1),
        reader.GetString(2),
        reader.GetString(3),
        reader.GetDouble(4),
        reader.GetDouble(5))
    {
        IndexedAt = await NullableField<DateTimeOffset>(reader, 6, cancellationToken),
        PageLastModifiedAt = await NullableField<DateTimeOffset>(reader, 7, cancellationToken),
        ProductVersion = await reader.IsDBNullAsync(8, cancellationToken) ? null : reader.GetString(8)
    };

    /// <summary>
    /// Reads a nullable value column. The page metadata comes from a LEFT JOIN, so it is SQL NULL for a page
    /// without a row, and Npgsql throws when a NULL is read as a value type.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="reader">The reader positioned on a row.</param>
    /// <param name="ordinal">The column ordinal.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The value, or null for SQL NULL.</returns>
    static async Task<T?> NullableField<T>(NpgsqlDataReader reader, int ordinal, CancellationToken cancellationToken)
        where T : struct =>
        await reader.IsDBNullAsync(ordinal, cancellationToken) ? null : await reader.GetFieldValueAsync<T>(ordinal, cancellationToken);
}
