// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_page_has_intro_and_sections : Specification
{
    const string Markdown =
        """
        # Event Sourcing

        Chronicle stores events as facts.

        ## Appending events

        Use the event log to append.

        ## Projections

        Projections build read models from events.
        """;

    Chunk[] _chunks = null!;

    void Because() => _chunks = [.. MarkdownChunker.Chunk(new PageUrl("https://cratis.io/chronicle/events.md"), Markdown)];

    [Fact] void should_produce_three_chunks() => _chunks.Length.ShouldEqual(3);
    [Fact] void should_use_page_title_for_intro_heading_path() => _chunks[0].HeadingPath.ShouldEqual("Event Sourcing");
    [Fact] void should_include_section_in_heading_path() => _chunks[1].HeadingPath.ShouldEqual("Event Sourcing > Appending events");
    [Fact] void should_keep_section_content() => _chunks[2].Content.ShouldContain("Projections build read models");
    [Fact] void should_give_every_chunk_a_unique_id() => _chunks.Select(chunk => chunk.Id).Distinct().Count().ShouldEqual(3);
    [Fact] void should_set_title_on_all_chunks() => _chunks.All(chunk => chunk.Title == "Event Sourcing").ShouldBeTrue();
}
