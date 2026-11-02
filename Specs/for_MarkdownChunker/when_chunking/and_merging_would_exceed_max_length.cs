// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_merging_would_exceed_max_length : Specification
{
    static readonly string Markdown = $"# Title\n\n## Group\n\nIntro.\n\n### A\n\n{new string('a', 290)}\n";

    Chunk[] _chunks = null!;

    void Because() => _chunks = [.. MarkdownChunker.Chunk(new PageUrl("https://cratis.io/long.md"), Markdown, maxChunkLength: 300)];

    [Fact] void should_not_merge() => _chunks.Length.ShouldEqual(2);
}
