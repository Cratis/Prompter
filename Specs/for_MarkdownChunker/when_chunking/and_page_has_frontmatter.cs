// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_page_has_frontmatter : Specification
{
    const string Markdown =
        """
        ---
        title: Getting started
        description: How to get started.
        ---

        # Getting started

        Install the packages.
        """;

    Chunk[] _chunks = null!;

    void Because() => _chunks = [.. MarkdownChunker.Chunk(new PageUrl("https://cratis.io/getting-started.md"), Markdown)];

    [Fact] void should_produce_a_single_chunk() => _chunks.Length.ShouldEqual(1);
    [Fact] void should_strip_the_frontmatter() => _chunks[0].Content.ShouldNotContain("description:");
    [Fact] void should_keep_the_body() => _chunks[0].Content.ShouldContain("Install the packages.");
}
