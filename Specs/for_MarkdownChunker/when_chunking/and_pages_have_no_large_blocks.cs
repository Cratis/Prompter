// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_pages_have_no_large_blocks : Specification
{
    Chunk[] _chunks = null!;

    void Because() => _chunks = [.. MarkdownChunker.Chunk(
        new PageUrl("https://cratis.io/plain.md"),
        "# Plain\n\nIntro line.\n\n## One\n\nFirst paragraph.\n\n| A | B |\n| --- | --- |\n| 1 | 2 |\n\n### Two\n\nSecond paragraph.\n")];

    [Fact] void should_keep_sequential_legacy_ids() => string.Join(',', _chunks.Select(chunk => chunk.Id.Value)).ShouldEqual("https://cratis.io/plain.md#0,https://cratis.io/plain.md#1,https://cratis.io/plain.md#2");
    [Fact] void should_keep_the_legacy_contents() => string.Join(";;", _chunks.Select(chunk => chunk.Content.Replace('\n', '/'))).ShouldEqual("Intro line.;;First paragraph.//| A | B |/| --- | --- |/| 1 | 2 |;;Second paragraph.");
    [Fact] void should_keep_the_legacy_hashes() => _chunks.All(chunk => chunk.Hash == ContentHash.For(Chunk.EmbeddingInputFor(chunk.Title, chunk.HeadingPath, chunk.Content))).ShouldBeTrue();
}
