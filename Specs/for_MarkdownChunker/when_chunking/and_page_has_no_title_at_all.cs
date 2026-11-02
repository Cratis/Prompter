// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_page_has_no_title_at_all : Specification
{
    const string Markdown = "---\ndescription: Every Cratis product in one table.\n---\n\n## Capability map\n\nChronicle and Arc.";

    Chunk[] _chunks = null!;

    void Because() => _chunks = [.. MarkdownChunker.Chunk(new PageUrl("https://cratis.io/cratis-stack.md"), Markdown)];

    [Fact] void should_fall_back_to_the_slug() => _chunks.All(chunk => chunk.Title == "cratis-stack").ShouldBeTrue();
}
