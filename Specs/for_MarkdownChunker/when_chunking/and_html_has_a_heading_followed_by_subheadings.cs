// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_html_has_a_heading_followed_by_subheadings : Specification
{
    Chunk[] _chunks = null!;

    void Because() => _chunks = [.. MarkdownChunker.Chunk(
        new PageUrl("https://cratis.io/wrong-choice/"),
        HtmlText.ToMarkdown("<html><head><title>Choice</title></head><body><main><h2>When Cratis is the wrong choice.</h2><h3>A</h3><p>short</p><h3>B</h3><p>short</p></main></body></html>"))];

    [Fact] void should_land_in_one_chunk() => _chunks.Length.ShouldEqual(1);
    [Fact] void should_carry_the_heading_in_the_path() => _chunks[0].HeadingPath.ShouldContain("When Cratis is the wrong choice.");
    [Fact] void should_keep_the_subheadings() => _chunks[0].Content.ShouldContain("### B");
}
