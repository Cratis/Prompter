// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_mdx_state_spans_lines : Specification
{
    const string Markdown = """
        # State

        Before {/* open
        hidden comment
        */}After

        <Card
          hidden attribute
        >Visible child
        </Card>Tail

        ```mdx
        <Card>
        {/* literal */}
        ## literal heading
        ```
        """;

    string _content = null!;

    void Because() => _content = string.Join('\n', MarkdownChunker.Chunk(new("https://cratis.io/state.md"), Markdown).Select(chunk => chunk.Content));

    [Fact] void should_keep_the_text_around_comments() => _content.ShouldContain("Before\nAfter");
    [Fact] void should_remove_comment_continuations() => _content.ShouldNotContain("hidden comment");
    [Fact] void should_remove_tag_continuations() => _content.ShouldNotContain("hidden attribute");
    [Fact] void should_keep_text_after_opening_and_closing_tags() => _content.ShouldContain("Visible child\nTail");
    [Fact] void should_leave_fenced_mdx_and_headings_untouched() => _content.ShouldContain("<Card>\n{/* literal */}\n## literal heading");
}
