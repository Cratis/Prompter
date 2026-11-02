// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_page_has_a_description : Specification
{
    const string Body = "## Capability map\n\nChronicle and Arc.";
    const string WithDescription = "---\ntitle: \"The Cratis Stack\"\ndescription: Every Cratis product in one table.\n---\n\n" + Body;
    const string WithoutDescription = "---\ntitle: \"The Cratis Stack\"\n---\n\n" + Body;
    static readonly PageUrl _page = new("https://cratis.io/cratis-stack.md");

    Chunk[] _chunks = null!;
    Chunk[] _withoutDescription = null!;

    void Because()
    {
        _chunks = [.. MarkdownChunker.Chunk(_page, WithDescription)];
        _withoutDescription = [.. MarkdownChunker.Chunk(_page, WithoutDescription)];
    }

    [Fact] void should_add_the_summary_chunk_last() => _chunks[^1].Id.Value.ShouldEqual("https://cratis.io/cratis-stack.md#summary");
    [Fact] void should_use_the_frontmatter_title() => _chunks[^1].Title.ShouldEqual("The Cratis Stack");
    [Fact] void should_use_a_summary_heading_path() => _chunks[^1].HeadingPath.ShouldEqual("The Cratis Stack > Summary");
    [Fact] void should_combine_the_unquoted_title_and_description() => _chunks[^1].Content.ShouldEqual("The Cratis Stack\n\nEvery Cratis product in one table.");
    [Fact] void should_hash_the_embedding_input() => _chunks[^1].Hash.ShouldEqual(ContentHash.For(Chunk.EmbeddingInputFor(_chunks[^1].Title, _chunks[^1].HeadingPath, _chunks[^1].Content)));
    [Fact] void should_not_add_a_summary_without_a_description() => _withoutDescription.Any(chunk => chunk.Id.Value.EndsWith("#summary", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_keep_body_chunks_byte_identical() => _chunks.Take(_chunks.Length - 1).SequenceEqual(_withoutDescription).ShouldBeTrue();
}
