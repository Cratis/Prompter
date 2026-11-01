// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_a_table_is_small : Specification
{
    const string Section = "Intro text.\n\n| A | B |\n| --- | --- |\n| 1 | 2 |\n| 3 | 4 |\n| 5 | 6 |";

    Chunk[] _chunks = null!;

    void Because() => _chunks = [.. MarkdownChunker.Chunk(new PageUrl("https://cratis.io/small.md"), $"# Small\n\n## Table\n\n{Section}\n")];

    [Fact] void should_produce_one_chunk() => _chunks.Length.ShouldEqual(1);
    [Fact] void should_keep_the_section_text_unchanged() => _chunks[0].Content.ShouldEqual(Section);
    [Fact] void should_keep_the_legacy_id() => _chunks[0].Id.Value.ShouldEqual("https://cratis.io/small.md#0");
}
