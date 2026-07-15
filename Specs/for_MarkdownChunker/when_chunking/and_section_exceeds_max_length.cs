// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_section_exceeds_max_length : Specification
{
    const int MaxLength = 120;

    Chunk[] _chunks = null!;

    string _markdown = null!;

    void Establish() =>
        _markdown = $"# Long page\n\n{string.Join("\n\n", Enumerable.Range(1, 6).Select(index => $"Paragraph {index} with some words in it."))}";

    void Because() => _chunks = [.. MarkdownChunker.Chunk(new PageUrl("https://cratis.io/long.md"), _markdown, MaxLength)];

    [Fact] void should_split_into_multiple_chunks() => _chunks.Length.ShouldBeGreaterThan(1);
    [Fact] void should_keep_every_chunk_within_the_limit() => _chunks.All(chunk => chunk.Content.Length <= MaxLength).ShouldBeTrue();
    [Fact] void should_keep_all_paragraphs() => string.Join("\n\n", _chunks.Select(chunk => chunk.Content)).ShouldContain("Paragraph 6");
    [Fact] void should_give_every_chunk_a_unique_id() => _chunks.Select(chunk => chunk.Id).Distinct().Count().ShouldEqual(_chunks.Length);
}
