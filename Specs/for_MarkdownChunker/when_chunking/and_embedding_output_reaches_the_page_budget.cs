// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_embedding_output_reaches_the_page_budget : Specification
{
    Chunk[] _chunks = null!;
    int _inputLength;

    void Because()
    {
        var markdown = $"# {new string('t', 200)}\n\n{new string('x', 10000)}\n";
        _inputLength = markdown.Length;
        _chunks = [.. MarkdownChunker.Chunk(new PageUrl("https://cratis.io/budget.md"), markdown, 1)];
    }

    [Fact] void should_stop_before_metadata_amplifies_the_output() => _chunks.Sum(chunk => (long)chunk.EmbeddingInput.Length).ShouldBeLessThan((_inputLength * 64L) + 8192);
    [Fact] void should_emit_the_affordable_prefix() => (_chunks.Length > 0 && _chunks.Length < 10000).ShouldBeTrue();
}
