// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

/// <summary>
/// The docs root is stored as /index.md, so a slug title would name every landing chunk "index".
/// A nested hero title must not make the top level title ambiguous.
/// </summary>
public class and_page_is_the_docs_root_without_a_heading : Specification
{
    const string Markdown =
        """
        ---
        title: Cratis documentation
        description: Cratis is an open-source (MIT) event sourcing and CQRS platform for .NET.
        template: splash
        hero:
          title: Software gets built differently now
        ---

        ## Cratis facts

        Cratis is an open-source (MIT) event sourcing and CQRS platform for .NET, built around the Chronicle event-sourcing database.
        """;

    Chunk[] _chunks = null!;

    void Because() => _chunks = [.. MarkdownChunker.Chunk(new PageUrl("https://www.cratis.io/index.md"), Markdown)];

    [Fact] void should_not_title_the_landing_page_index() => _chunks.Any(chunk => chunk.Title == "index").ShouldBeFalse();
    [Fact] void should_use_the_top_level_title() => _chunks[0].Title.ShouldEqual("Cratis documentation");
    [Fact] void should_root_the_facts_in_the_title() => _chunks[0].HeadingPath.ShouldEqual("Cratis documentation > Cratis facts");
}
