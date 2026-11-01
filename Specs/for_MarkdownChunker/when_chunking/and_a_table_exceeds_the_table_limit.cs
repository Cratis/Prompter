// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_a_table_exceeds_the_table_limit : Specification
{
    const string Header = "| Name | Description |";
    const string Separator = "| --- | --- |";

    Chunk[] _chunks = null!;
    string[] _rows = null!;

    IEnumerable<Chunk> TableChunks => _chunks.Where(chunk => chunk.Content.Contains(Header, StringComparison.Ordinal) && chunk.Content.Contains("Item ", StringComparison.Ordinal));

    void Establish()
    {
        _rows = [.. Enumerable.Range(1, 30).Select(index => $"| Item {index:00} | A description of item {index:00} that runs on a while |")];
    }

    void Because()
    {
        var markdown = $"# Products\n\n## Catalogue\n\nThe catalogue lists every item.\n\n{Header}\n{Separator}\n{string.Join('\n', _rows)}\n";
        _chunks = [.. MarkdownChunker.Chunk(new PageUrl("https://cratis.io/products.md"), markdown)];
    }

    [Fact] void should_split_the_table_into_more_than_one_chunk() => TableChunks.Count().ShouldBeGreaterThan(1);
    [Fact] void should_keep_the_heading_path_on_every_group() => TableChunks.All(chunk => chunk.HeadingPath == "Products > Catalogue").ShouldBeTrue();
    [Fact] void should_repeat_the_header_and_separator() => TableChunks.All(chunk => chunk.Content.Contains($"{Header}\n{Separator}\n", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_every_row_once_and_in_order() => string.Join('\n', TableChunks.SelectMany(chunk => chunk.Content.Split('\n').Where(line => line.StartsWith("| Item", StringComparison.Ordinal)))).ShouldEqual(string.Join('\n', _rows));
    [Fact] void should_keep_every_chunk_within_the_table_limit() => _chunks.All(chunk => chunk.Content.Length <= 1500).ShouldBeTrue();
    [Fact] void should_give_every_chunk_a_unique_id() => _chunks.Select(chunk => chunk.Id).Distinct().Count().ShouldEqual(_chunks.Length);
    [Fact] void should_number_the_pieces_after_the_legacy_part() => TableChunks.First().Id.Value.EndsWith("#0.1", StringComparison.Ordinal).ShouldBeTrue();
}
