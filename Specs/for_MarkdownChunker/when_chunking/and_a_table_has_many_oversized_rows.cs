// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_a_table_has_many_oversized_rows : Specification
{
    Chunk[] _chunks = null!;
    int _inputLength;

    void Because()
    {
        var header = $"| {new string('h', 400)} | B |\n| --- | --- |";
        var rows = string.Join('\n', Enumerable.Range(0, 200).Select(index => $"| r{index} | {new string('v', 9000)} |"));
        var markdown = $"# Rows\n\n{header}\n{rows}\n";
        _inputLength = markdown.Length;
        _chunks = [.. MarkdownChunker.Chunk(new PageUrl("https://cratis.io/rows.md"), markdown)];
    }

    [Fact] void should_keep_every_chunk_within_the_limit() => _chunks.All(chunk => chunk.Content.Length <= 4000).ShouldBeTrue();
    [Fact] void should_bound_total_output_by_a_small_multiple_of_the_input() => _chunks.Sum(chunk => chunk.Content.Length).ShouldBeLessThan(_inputLength * 2);
}
