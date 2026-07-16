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
                   COALESCE(1.0 / ({RrfConstant} + lexical.rank), 0) AS score
            FROM chunks c
            LEFT JOIN semantic ON c.id = semantic.id
            LEFT JOIN lexical ON c.id = lexical.id
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
            passages.Add(new(
                new PageUrl(reader.GetString(0)),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetDouble(4)));
        }

        return passages;
    }
}
