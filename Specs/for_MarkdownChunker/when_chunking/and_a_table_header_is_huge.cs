// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_a_table_header_is_huge : Specification
{
    const int Rows = 100000;

    Chunk[] _chunks = null!;
    int _inputLength;

    void Because()
    {
        var header = $"| {new string('h', 500000)} | B |\n| --- | --- |";
        var markdown = $"# Big\n\n{header}\n{string.Join('\n', Enumerable.Range(0, Rows).Select(index => $"| r{index} | v |"))}\n";
        _inputLength = markdown.Length;
        _chunks = [.. MarkdownChunker.Chunk(new PageUrl("https://cratis.io/big.md"), markdown)];
    }

    [Fact] void should_keep_every_chunk_within_the_limit() => _chunks.All(chunk => chunk.Content.Length <= 4000).ShouldBeTrue();
    [Fact] void should_not_repeat_the_huge_header() => _chunks.Sum(chunk => chunk.Content.Count(character => character == 'h')).ShouldEqual(500000);
    [Fact] void should_keep_total_output_linear_in_the_input() => _chunks.Sum(chunk => chunk.Content.Length).ShouldBeLessThan(_inputLength * 2);
}
