// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_metadata_would_be_repeated_across_many_sections : Specification
{
    Chunk[] _chunks = null!;
    int _inputLength;

    void Because()
    {
        var markdown = $"# {new string('t', 20000)}\n\n## {new string('h', 20000)}\n\n{string.Concat(Enumerable.Range(0, 1000).Select(index => $"### Section {index}\n\nBody. {new string('b', 200)}\n\n"))}";
        _inputLength = markdown.Length;
        _chunks = [.. MarkdownChunker.Chunk(new PageUrl("https://cratis.io/metadata.md"), markdown)];
    }

    [Fact] void should_keep_every_section() => _chunks.Length.ShouldEqual(1000);
    [Fact] void should_bound_title_before_repeating_it() => _chunks.All(chunk => chunk.Title.Length <= 200).ShouldBeTrue();
    [Fact] void should_bound_heading_components_before_building_paths() => _chunks.All(chunk => chunk.HeadingPath.Length <= 606).ShouldBeTrue();
    [Fact] void should_bound_aggregate_embedding_input_not_only_body_text() => _chunks.Sum(chunk => (long)chunk.EmbeddingInput.Length).ShouldBeLessThan(_inputLength * 16L);
    [Fact] void should_hash_exactly_the_bounded_embedding_input() => _chunks.All(chunk => chunk.Hash == ContentHash.For(chunk.EmbeddingInput)).ShouldBeTrue();
}
