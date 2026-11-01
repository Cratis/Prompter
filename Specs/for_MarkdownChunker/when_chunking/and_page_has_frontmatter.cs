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

    [Fact] void should_produce_the_body_and_a_summary_chunk() => _chunks.Length.ShouldEqual(2);
    [Fact] void should_strip_the_frontmatter() => _chunks.Any(chunk => chunk.Content.Contains("description:")).ShouldBeFalse();
    [Fact] void should_keep_the_body() => _chunks[0].Content.ShouldContain("Install the packages.");
    [Fact] void should_not_leak_the_title_key() => _chunks.Any(chunk => chunk.Content.Contains("title:")).ShouldBeFalse();
}
