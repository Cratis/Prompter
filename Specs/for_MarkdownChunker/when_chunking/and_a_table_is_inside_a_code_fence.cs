// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_a_table_is_inside_a_code_fence : Specification
{
    Chunk[] _chunks = null!;

    void Because()
    {
        var rows = string.Join('\n', Enumerable.Range(1, 30).Select(index => $"| Item {index:00} | A description of item {index:00} that runs on a while |"));
        var markdown = $"# Fence\n\n## Sample\n\n```markdown\n| Name | Description |\n| --- | --- |\n{rows}\n```\n";
        _chunks = [.. MarkdownChunker.Chunk(new PageUrl("https://cratis.io/fence.md"), markdown)];
    }

    [Fact] void should_stay_one_chunk() => _chunks.Length.ShouldEqual(1);
    [Fact] void should_not_repeat_the_header() => _chunks[0].Content.Split("| Name | Description |").Length.ShouldEqual(2);
}
