// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_a_single_line_exceeds_max_length : Specification
{
    const int MaxLength = 120;

    Chunk[] _chunks = null!;
    string _line = null!;

    void Establish() => _line = string.Concat(Enumerable.Range(0, 300).Select(index => (char)('a' + (index % 26))));

    void Because() => _chunks = [.. MarkdownChunker.Chunk(new PageUrl("https://cratis.io/line.md"), $"# Line\n\n{_line}\n", MaxLength)];

    [Fact] void should_keep_every_chunk_within_the_limit() => _chunks.All(chunk => chunk.Content.Length <= MaxLength).ShouldBeTrue();
    [Fact] void should_split_into_three_chunks() => _chunks.Length.ShouldEqual(3);
    [Fact] void should_not_lose_characters() => string.Concat(_chunks.Select(chunk => chunk.Content)).ShouldEqual(_line);
}
