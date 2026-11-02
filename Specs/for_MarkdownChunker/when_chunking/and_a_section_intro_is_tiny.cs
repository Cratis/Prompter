// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_a_section_intro_is_tiny : Specification
{
    static readonly string _markdown = $"# Title\n\n## Strengths\n\nCratis is good at a few things.\n\n### Events\n\n{new string('e', 600)}\n\n### Arc\n\n{new string('a', 600)}\n";

    Chunk[] _chunks = null!;

    void Because() => _chunks = [.. MarkdownChunker.Chunk(new PageUrl("https://cratis.io/why.md"), _markdown)];

    [Fact] void should_produce_two_chunks() => _chunks.Length.ShouldEqual(2);
    [Fact] void should_use_the_section_path_for_the_merged_chunk() => _chunks[0].HeadingPath.ShouldEqual("Title > Strengths");
    [Fact] void should_keep_the_intro_in_the_merged_chunk() => _chunks[0].Content.ShouldContain("Cratis is good at a few things.");
    [Fact] void should_keep_the_subheading_inline() => _chunks[0].Content.ShouldContain("### Events");
    [Fact] void should_leave_the_large_sibling_alone() => _chunks[1].HeadingPath.ShouldEqual("Title > Strengths > Arc");
}
