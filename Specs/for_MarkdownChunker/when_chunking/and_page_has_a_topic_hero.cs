// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

/// <summary>
/// Fixture is the real cratis.io Arc page mirror (https://cratis.io/arc.md, captured 2026-07-15), whose
/// TopicHero wraps prose that must survive the tag stripping.
/// </summary>
public class and_page_has_a_topic_hero : Specification
{
    string _content = null!;

    void Because()
    {
        var chunks = MarkdownChunker.Chunk(new PageUrl("https://cratis.io/arc.md"), Fixture.Load("arc.md"));
        _content = string.Join("\n\n", chunks.Select(chunk => chunk.Content));
    }

    [Fact] void should_strip_the_topic_hero_open_tag() => _content.ShouldNotContain("<TopicHero");
    [Fact] void should_strip_the_topic_hero_close_tag() => _content.ShouldNotContain("</TopicHero");
    [Fact] void should_keep_the_hero_prose() => _content.ShouldContain("Turn **commands** and **queries**");
    [Fact] void should_strip_card_grids() => _content.ShouldNotContain("<CardGrid");
    [Fact] void should_keep_card_prose() => _content.ShouldContain("Build a backend slice end to end");
}
