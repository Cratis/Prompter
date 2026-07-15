// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_page_has_a_multiline_component : Specification
{
    const string Markdown =
        """
        # Overview

        <StackJourney
          eyebrow="One feature"
          title="See the whole loop"
          intro="Some intro text in an attribute."
        />

        Prose after the component stays.
        """;

    string _content = null!;

    void Because()
    {
        var chunks = MarkdownChunker.Chunk(new PageUrl("https://cratis.io/overview.md"), Markdown);
        _content = string.Join("\n\n", chunks.Select(chunk => chunk.Content));
    }

    [Fact] void should_strip_the_opening_tag() => _content.ShouldNotContain("<StackJourney");
    [Fact] void should_strip_the_orphaned_attribute_lines() => _content.ShouldNotContain("eyebrow=");
    [Fact] void should_keep_prose_after_the_component() => _content.ShouldContain("Prose after the component stays.");
}
