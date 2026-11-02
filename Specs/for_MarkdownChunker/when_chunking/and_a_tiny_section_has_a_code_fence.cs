// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_a_tiny_section_has_a_code_fence : Specification
{
    const string Markdown = "# Title\n\n## Group\n\nIntro.\n\n### A\n\n```csharp\nvar a = 1;\n```\n\n### B\n\nTiny.\n";

    Chunk[] _chunks = null!;

    void Because() => _chunks = [.. MarkdownChunker.Chunk(new PageUrl("https://cratis.io/fence.md"), Markdown)];

    [Fact] void should_not_merge_the_fenced_section() => _chunks.Select(chunk => chunk.HeadingPath).ShouldEqual(["Title > Group", "Title > Group > A", "Title > Group > B"]);
}
