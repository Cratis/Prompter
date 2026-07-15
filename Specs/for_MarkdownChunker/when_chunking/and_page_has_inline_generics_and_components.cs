// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_page_has_inline_generics_and_components : Specification
{
    const string Markdown =
        """
        # Guide

        Call `readModels.GetInstances<Author>()` to load every author.

        <CardGrid>
          <SimpleCard title="Load" icon="db" link="/load">
            Card body prose stays.
          </SimpleCard>
        </CardGrid>
        """;

    string _content = null!;

    void Because()
    {
        var chunks = MarkdownChunker.Chunk(new PageUrl("https://cratis.io/guide.md"), Markdown);
        _content = string.Join("\n\n", chunks.Select(chunk => chunk.Content));
    }

    [Fact] void should_keep_inline_generics_in_prose() => _content.ShouldContain("GetInstances<Author>()");
    [Fact] void should_strip_the_card_grid() => _content.ShouldNotContain("<CardGrid");
    [Fact] void should_strip_the_simple_card() => _content.ShouldNotContain("<SimpleCard");
    [Fact] void should_keep_the_card_body_prose() => _content.ShouldContain("Card body prose stays.");
}
