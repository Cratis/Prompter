// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_tiny_sections_are_in_different_groups : Specification
{
    const string Markdown = "# Title\n\nIntro.\n\n## One\n\nFirst.\n\n## Two\n\nSecond.\n";

    Chunk[] _chunks = null!;

    void Because() => _chunks = [.. MarkdownChunker.Chunk(new PageUrl("https://cratis.io/groups.md"), Markdown)];

    [Fact] void should_keep_every_section_separate() => _chunks.Length.ShouldEqual(3);
    [Fact] void should_keep_the_paths() => _chunks.Select(chunk => chunk.HeadingPath).ShouldEqual(["Title", "Title > One", "Title > Two"]);
}
