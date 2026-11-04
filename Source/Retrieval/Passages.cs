// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Ingestion;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Npgsql;
using Pgvector;

namespace Cratis.Prompter.Retrieval;

/// <summary>
/// Represents an implementation of <see cref="IPassages"/> doing hybrid retrieval with full-text
/// and vector ranks fused with reciprocal rank fusion (RRF), then bounded summary-page expansion.
/// </summary>
/// <param name="dataSource">The Postgres data source.</param>
/// <param name="embeddings">The generator used to embed the question.</param>
/// <param name="options">The Prompter options, for the documentation site URL a product scope is resolved against.</param>
public class Passages(
    NpgsqlDataSource dataSource,
    IEmbeddingGenerator<string, Embedding<float>> embeddings,
    IOptions<PrompterOptions> options) : IPassages
{
    /// <summary>
    /// The largest hybrid candidate pool one search can return (semantic and lexical arms of 20 each).
    /// </summary>
    public const int MaximumCandidates = CandidatesPerMethod * 2;

    const int CandidatesPerMethod = 20;
    const int RrfConstant = 60;
    const int MaximumSectionAnchors = 3;
    const int SmallPageBytes = 4000;
    const int MaximumSectionRows = 64;

    /// <summary>Keeps every candidate when no prefix is given, otherwise only pages under one of the prefixes.</summary>
    const string ScopeFilter = "(cardinality($4::text[]) = 0 OR EXISTS (SELECT 1 FROM unnest($4::text[]) AS prefix WHERE starts_with(page_url, prefix)))";

    /// <summary>A curated docs overview set: the canonical answer to broad Cratis questions per the system prompt's source rule.</summary>
    static readonly string[] _overviewPaths = ["index.md", "why-cratis.md", "cratis-stack.md"];

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
                WHERE {ScopeFilter}
                ORDER BY embedding <=> $1
                LIMIT {CandidatesPerMethod}
            ),
            lexical AS (
                SELECT id, ROW_NUMBER() OVER (
                           ORDER BY (tsv @@ websearch_to_tsquery('english', $2)) DESC,
                                    ts_rank_cd(tsv, websearch_to_tsquery('english', $2)) DESC,
                                    ts_rank_cd(tsv, websearch_to_tsquery('english', $5), 1) DESC, id) AS rank
                FROM chunks
                WHERE (tsv @@ websearch_to_tsquery('english', $2) OR tsv @@ websearch_to_tsquery('english', $5)) AND {ScopeFilter}
                ORDER BY rank
                LIMIT {CandidatesPerMethod}
            )
            SELECT c.page_url, c.title, c.heading_path, c.content,
                   COALESCE(1.0 / ({RrfConstant} + semantic.rank), 0) +
                   COALESCE(1.0 / ({RrfConstant} + lexical.rank), 0) AS score,
                   1.0 - (c.embedding <=> $1) AS semantic_similarity,
                   p.indexed_at, p.page_last_modified, p.product_version, c.id = c.page_url || '#summary' AS is_summary, c.id
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
        command.Parameters.AddWithValue(ScopePrefixes(question));
        command.Parameters.AddWithValue(LexicalQuery.Lenient(question.Value));

        var passages = new List<Passage>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            passages.Add(await ReadPassage(reader, cancellationToken));
        }

        await reader.CloseAsync();
        if (question.Scope is null && OverviewQuestions.Matches(question.Value))
        {
            var overview = await OverviewSummaries(questionEmbedding, passages.Count > 0 ? passages[0].Score : 0.0, cancellationToken);
            var overviewIds = overview.Select(passage => passage.ChunkId).ToHashSet(StringComparer.Ordinal);
            passages = [.. overview, .. passages.Where(passage => !overviewIds.Contains(passage.ChunkId))];
        }

        var admittedSummaries = passages
            .Where(passage => SourceHosts.IsCitable(passage.Page, options.Value) && passage.ClearsFloor(options.Value.Answering.MinSemanticSimilarity))
            .Take(options.Value.Answering.MaxPassages).Where(passage => passage.IsSummary).ToArray();
        var summaryPages = admittedSummaries.Where(passage => !passage.IsCurated).Select(passage => passage.Page.Value).Distinct().ToArray();
        if (summaryPages.Length > 0)
        {
            passages.AddRange(await ExpandSummaries(questionEmbedding, summaryPages, false, cancellationToken));
        }

        // A curated overview page is relevant because the question matched the overview intent, so its best body
        // chunks are read without the similarity floor; the answering layer still applies the expansion and total caps.
        var curatedPages = admittedSummaries.Where(passage => passage.IsCurated).Select(passage => passage.Page.Value)
            .Except(summaryPages, StringComparer.Ordinal).Distinct().ToArray();
        if (curatedPages.Length > 0)
        {
            passages.AddRange(await ExpandSummaries(questionEmbedding, curatedPages, true, cancellationToken));
        }

        var anchors = SectionAnchors(passages);
        if (anchors.Length > 0)
        {
            passages.AddRange(await ExpandSections(questionEmbedding, anchors, cancellationToken));
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
        ProductVersion = await reader.IsDBNullAsync(8, cancellationToken) ? null : reader.GetString(8),
        IsSummary = reader.GetBoolean(9),
        ChunkId = reader.GetString(10)
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

    string[] OverviewSummaryIds()
    {
        var docs = new Uri(options.Value.DocsSiteUrl).Host;
        var twin = docs.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? docs[4..] : $"www.{docs}";
        return [.. new[] { docs, twin }.SelectMany(host => _overviewPaths.Select(path => $"https://{host}/{path}#summary"))];
    }

    async Task<List<Passage>> OverviewSummaries(Vector questionEmbedding, double score, CancellationToken cancellationToken)
    {
        // Reuse the question embedding; the answering layer still applies the similarity threshold and the cap.
        await using var command = dataSource.CreateCommand(
            """
            SELECT c.page_url, c.title, c.heading_path, c.content, $3::double precision AS score,
                   1.0 - (c.embedding <=> $1) AS semantic_similarity,
                   p.indexed_at, p.page_last_modified, p.product_version, true AS is_summary, c.id
            FROM chunks c
            LEFT JOIN documentation_pages p ON p.page_url = c.page_url
            WHERE c.id = ANY($2::text[])
            ORDER BY c.embedding <=> $1, c.id
            """);
        command.Parameters.AddWithValue(questionEmbedding);
        command.Parameters.AddWithValue(OverviewSummaryIds());
        command.Parameters.AddWithValue(score);
        var summaries = new List<Passage>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            summaries.Add(await ReadPassage(reader, cancellationToken) with { IsCurated = true });
        }

        return summaries;
    }

    async Task<List<Passage>> ExpandSummaries(Vector questionEmbedding, string[] pages, bool curated, CancellationToken cancellationToken)
    {
        // Reuse the question embedding. Only the best two non-summary chunks from admitted pages
        // compete for reserved slots; the answering layer still enforces grounding and the total cap.
        await using var command = dataSource.CreateCommand(
            """
            SELECT c.page_url, c.title, c.heading_path, c.content, 0.0::double precision AS score,
                   1.0 - (c.embedding <=> $1) AS semantic_similarity,
                   p.indexed_at, p.page_last_modified, p.product_version, false AS is_summary, c.id
            FROM chunks c
            LEFT JOIN documentation_pages p ON p.page_url = c.page_url
            WHERE c.page_url = ANY($2::text[]) AND c.id <> c.page_url || '#summary'
              AND 1.0 - (c.embedding <=> $1) >= $3
              AND 1.0 - (c.embedding <=> $1) <= 1
            ORDER BY c.embedding <=> $1, c.id
            LIMIT 2
            """);
        command.Parameters.AddWithValue(questionEmbedding);
        command.Parameters.AddWithValue(pages);
        command.Parameters.AddWithValue(curated ? -1.0 : options.Value.Answering.MinSemanticSimilarity);
        var expansion = new List<Passage>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            expansion.Add(await ReadPassage(reader, cancellationToken) with { IsPageExpansion = true, IsCurated = curated });
        }
        return expansion;
    }

    /// <summary>
    /// Picks the admitted, non-summary passages whose section is read whole, one per distinct section.
    /// </summary>
    /// <param name="passages">The main search rows, best first.</param>
    /// <returns>The anchors, best first.</returns>
    SectionAnchor[] SectionAnchors(List<Passage> passages) => [.. passages
        .Where(passage => SourceHosts.IsCitable(passage.Page, options.Value) && passage.ClearsFloor(options.Value.Answering.MinSemanticSimilarity))
        .Take(options.Value.Answering.MaxPassages)
        .Where(passage => !passage.IsSummary && passage.ChunkId is not null)
        .Select(SectionAnchor.Of)
        .DistinctBy(anchor => (anchor.Page, anchor.Root))
        .Take(MaximumSectionAnchors)];

    async Task<List<Passage>> ExpandSections(Vector questionEmbedding, SectionAnchor[] anchors, CancellationToken cancellationToken)
    {
        // Only the anchors are excluded. Other already retrieved chunks may return: one below the similarity threshold was
        // not admitted, and SectionExpansions removes the admitted ones. Rows are ranked before the limit, so the cut drops the
        // least relevant section. A chunk joins when it is in the anchor's section, next to the anchor, or on a small page. No similarity
        // threshold applies: the rows only ever attach to an admitted anchor, and the answering layer bounds the bytes.
        await using var command = dataSource.CreateCommand(
            $"""
            WITH anchors AS (
                SELECT page, root, exact FROM unnest($2::text[], $3::text[], $4::boolean[]) AS a(page, root, exact)
            ),
            sizes AS (
                SELECT page_url, SUM(octet_length(content)) AS total
                FROM chunks
                WHERE page_url = ANY($2::text[]) AND id <> page_url || '#summary'
                GROUP BY page_url
            )
            SELECT c.page_url, c.title, c.heading_path, c.content, 0.0::double precision AS score,
                   1.0 - (c.embedding <=> $1) AS semantic_similarity,
                   p.indexed_at, p.page_last_modified, p.product_version, false AS is_summary, c.id
            FROM chunks c
            JOIN sizes s ON s.page_url = c.page_url
            LEFT JOIN documentation_pages p ON p.page_url = c.page_url
            WHERE c.page_url = ANY($2::text[]) AND c.id <> c.page_url || '#summary' AND c.id <> ALL($6::text[])
              AND (s.total <= {SmallPageBytes}
                   OR c.id = ANY($5::text[])
                   OR EXISTS (SELECT 1 FROM anchors a WHERE a.page = c.page_url
                              AND (c.heading_path = a.root OR (NOT a.exact AND starts_with(c.heading_path, a.root || ' > ')))))
            ORDER BY array_position($2::text[], c.page_url),
                     (c.id = ANY($5::text[]) OR EXISTS (SELECT 1 FROM anchors a WHERE a.page = c.page_url
                         AND (c.heading_path = a.root OR (NOT a.exact AND starts_with(c.heading_path, a.root || ' > '))))) DESC,
                     (c.id = ANY($5::text[])) DESC,
                     COALESCE(substring(c.id from '#([0-9]+)(?:\.[0-9]+)?$')::int, 2147483647),
                     COALESCE(substring(c.id from '#[0-9]+\.([0-9]+)$')::int, 0),
                     c.id
            LIMIT {MaximumSectionRows}
            """);
        command.Parameters.AddWithValue(questionEmbedding);
        command.Parameters.AddWithValue(anchors.Select(anchor => anchor.Page).ToArray());
        command.Parameters.AddWithValue(anchors.Select(anchor => anchor.Root).ToArray());
        command.Parameters.AddWithValue(anchors.Select(anchor => anchor.Exact).ToArray());
        command.Parameters.AddWithValue(anchors.SelectMany(anchor => anchor.Neighbors).ToArray());
        command.Parameters.AddWithValue(anchors.Select(anchor => anchor.ChunkId).ToArray());
        var rows = new List<Passage>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(await ReadPassage(reader, cancellationToken) with { IsSectionExpansion = true });
        }

        return [.. rows.OrderBy(row => SectionAnchor.Rank(anchors, row)).ThenBy(row => SectionAnchor.Distance(anchors, row))
            .ThenBy(row => ChunkOrder.Of(row.ChunkId).Piece)];
    }

    string[] ScopePrefixes(Question question) => question.Scope is { } product
        ? [.. ProductScope.PrefixesFor(product, options.Value.DocsSiteUrl)]
        : [];
}
