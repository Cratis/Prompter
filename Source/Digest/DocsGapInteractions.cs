// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Npgsql;

namespace Cratis.Prompter.Digest;

/// <summary>
/// Reads only the stored metadata needed for gap counts, without persisting any new data.
/// </summary>
/// <param name="dataSource">The database connection pool.</param>
public class DocsGapInteractions(NpgsqlDataSource dataSource) : IDocsGapInteractions
{
    const string Query = """
        SELECT source, cited_pages, was_refusal, feedback,
          CASE category WHEN 1 THEN 'answered' WHEN 2 THEN 'docs-gap' WHEN 3 THEN 'missing-capability'
            WHEN 4 THEN 'likely-bug' WHEN 5 THEN 'out-of-scope' END,
          CASE product WHEN 1 THEN 'chronicle' WHEN 2 THEN 'arc' WHEN 3 THEN 'fundamentals'
            WHEN 4 THEN 'components' WHEN 5 THEN 'cli' END
        FROM interactions
        WHERE occurred_at >= $1 AND occurred_at < $2
          AND (was_refusal OR feedback = 'down' OR category IN (2,3,4))
        LIMIT 10000
        """;

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DocsGapRow>> Read(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(Query);
        command.Parameters.AddWithValue(from.ToUniversalTime());
        command.Parameters.AddWithValue(to.ToUniversalTime());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rows = new List<DocsGapRow>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var pages = await reader.GetFieldValueAsync<string[]>(1, cancellationToken);
            var feedback = await reader.IsDBNullAsync(3, cancellationToken) ? null : reader.GetString(3);
            var category = await reader.IsDBNullAsync(4, cancellationToken) ? null : reader.GetString(4);
            var product = await reader.IsDBNullAsync(5, cancellationToken) ? null : reader.GetString(5);
            rows.Add(new(reader.GetString(0), pages, reader.GetBoolean(2), feedback, category, product));
        }

        return rows;
    }
}
