// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

/// <summary>
/// Fixture is the real cratis.io landing page mirror (https://cratis.io/index.md, captured 2026-07-15),
/// exercising the full range of MDX constructs the mirrors leak: module imports, JSX comments, self-closing,
/// multi-line and paired Starlight components.
/// </summary>
public class and_page_is_the_real_landing_page : Specification
{
    string _content = null!;

    void Because()
    {
        var chunks = MarkdownChunker.Chunk(new PageUrl("https://cratis.io/index.md"), Fixture.Load("landing-page.md"));
        _content = string.Join("\n\n", chunks.Select(chunk => chunk.Content));
    }

    [Fact] void should_strip_card_grids() => _content.ShouldNotContain("<CardGrid");
    [Fact] void should_strip_simple_cards() => _content.ShouldNotContain("<SimpleCard");
    [Fact] void should_strip_link_cards() => _content.ShouldNotContain("<LinkCard");
    [Fact] void should_strip_the_self_closing_rotating_hero() => _content.ShouldNotContain("<RotatingHero");
    [Fact] void should_strip_the_multiline_stack_journey() => _content.ShouldNotContain("<StackJourney");
    [Fact] void should_strip_tabs() => _content.ShouldNotContain("<Tabs");
    [Fact] void should_strip_tab_items() => _content.ShouldNotContain("<TabItem");
    [Fact] void should_strip_steps() => _content.ShouldNotContain("<Steps");
    [Fact] void should_strip_jsx_comments() => _content.ShouldNotContain("{/*");
    [Fact] void should_strip_module_imports() => _content.ShouldNotContain("@astrojs/starlight");
    [Fact] void should_not_leak_self_closing_tag_attributes() => _content.ShouldNotContain("href=");
    [Fact] void should_keep_card_description_prose() => _content.ShouldContain("Domain model, identity, tenant context");
    [Fact] void should_keep_step_prose() => _content.ShouldContain("Install the Cratis templates");
    [Fact] void should_keep_code_examples() => _content.ShouldContain("RegisterAuthor");
    [Fact] void should_keep_jsx_inside_code_examples() => _content.ShouldContain("<CommandDialog");
}
