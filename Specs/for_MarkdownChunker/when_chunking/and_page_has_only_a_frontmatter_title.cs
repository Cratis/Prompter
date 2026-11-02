// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

/// <summary>
/// The cratis.io markdown mirrors carry the page title only in frontmatter and have no level one heading.
/// </summary>
public class and_page_has_only_a_frontmatter_title : Specification
{
    const string Markdown =
        """
        ---
        title: Why developers choose Cratis
        description: "The practical reasons teams choose Cratis."
        ---

        **Build the system you modeled**

        ## When Cratis is a good fit — and when it isn't

        Because the products are separable, "is Cratis a good fit?" is really a set of connected questions.
        """;

    Chunk[] _chunks = null!;

    void Because() => _chunks = [.. MarkdownChunker.Chunk(new PageUrl("https://www.cratis.io/why-cratis.md"), Markdown)];

    [Fact] void should_title_every_chunk_with_the_frontmatter_title() => _chunks.All(chunk => chunk.Title == "Why developers choose Cratis").ShouldBeTrue();
    [Fact] void should_root_the_heading_path_in_the_frontmatter_title() => _chunks.Select(chunk => chunk.HeadingPath).ShouldContain("Why developers choose Cratis > When Cratis is a good fit — and when it isn't");
    [Fact] void should_root_the_summary_in_the_frontmatter_title() => _chunks[^1].HeadingPath.ShouldEqual("Why developers choose Cratis > Summary");
    [Fact] void should_embed_the_frontmatter_title() => _chunks.All(chunk => chunk.EmbeddingInput.StartsWith("Why developers choose Cratis — ", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_hash_the_embedding_input() => _chunks.All(chunk => chunk.Hash == ContentHash.For(chunk.EmbeddingInput)).ShouldBeTrue();
}
