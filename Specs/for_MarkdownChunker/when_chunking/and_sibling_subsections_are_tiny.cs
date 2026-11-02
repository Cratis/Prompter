// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_sibling_subsections_are_tiny : Specification
{
    const string Markdown = "# Title\n\n## Choices\n\n### One\n\nFirst.\n\n### Two\n\nSecond.\n\n### Three\n\nThird.\n";

    Chunk[] _chunks = null!;

    void Because() => _chunks = [.. MarkdownChunker.Chunk(new PageUrl("https://cratis.io/choices.md"), Markdown)];

    [Fact] void should_produce_one_chunk() => _chunks.Length.ShouldEqual(1);
    [Fact] void should_use_the_common_heading_path() => _chunks[0].HeadingPath.ShouldEqual("Title > Choices");
    [Fact] void should_keep_every_heading_inline() => new[] { "### One", "### Two", "### Three" }.All(_chunks[0].Content.Contains).ShouldBeTrue();
    [Fact] void should_keep_every_body() => new[] { "First.", "Second.", "Third." }.All(_chunks[0].Content.Contains).ShouldBeTrue();
}
