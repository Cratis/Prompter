// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_a_fence_spans_paragraph_parts : Specification
{
    Chunk[] _chunks = null!;

    void Because()
    {
        var rows = string.Join('\n', Enumerable.Range(1, 30).Select(index => $"| Item {index:00} | A description of item {index:00} that runs on a while |"));
        var markdown = $"# Span\n\n## Sample\n\n```markdown\n{new string('i', 3000)}\n\n| Name | Description |\n| --- | --- |\n{rows}\n```\n\nAfter the fence.\n";
        _chunks = [.. MarkdownChunker.Chunk(new PageUrl("https://cratis.io/span.md"), markdown, 4000)];
    }

    [Fact] void should_not_treat_the_fenced_table_as_a_table() => _chunks.SelectMany(chunk => chunk.Content.Split('\n')).Count(line => line == "| Name | Description |").ShouldEqual(1);
    [Fact] void should_keep_every_row_once() => _chunks.SelectMany(chunk => chunk.Content.Split('\n')).Count(line => line.StartsWith("| Item", StringComparison.Ordinal)).ShouldEqual(30);
}
