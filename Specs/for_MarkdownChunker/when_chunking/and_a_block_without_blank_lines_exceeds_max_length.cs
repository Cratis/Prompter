// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_a_block_without_blank_lines_exceeds_max_length : Specification
{
    const int MaxLength = 120;

    Chunk[] _chunks = null!;
    string[] _lines = null!;

    void Establish() => _lines = [.. Enumerable.Range(1, 40).Select(index => $"Line {index:00} with a few words in it.")];

    void Because() => _chunks = [.. MarkdownChunker.Chunk(new PageUrl("https://cratis.io/block.md"), $"# Block\n\n{string.Join("\n", _lines)}\n", MaxLength)];

    [Fact] void should_split_into_multiple_chunks() => _chunks.Length.ShouldBeGreaterThan(1);
    [Fact] void should_keep_every_chunk_within_the_limit() => _chunks.All(chunk => chunk.Content.Length <= MaxLength).ShouldBeTrue();
    [Fact] void should_keep_every_line_in_order() => string.Join("\n", _chunks.Select(chunk => chunk.Content)).ShouldEqual(string.Join("\n", _lines));
    [Fact] void should_give_every_chunk_a_unique_id() => _chunks.Select(chunk => chunk.Id).Distinct().Count().ShouldEqual(_chunks.Length);
}
