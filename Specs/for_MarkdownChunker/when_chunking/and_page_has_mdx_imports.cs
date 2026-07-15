// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_page_has_mdx_imports : Specification
{
    const string Markdown =
        """
        ---
        title: Arc
        ---

        import { CardGrid } from '@astrojs/starlight/components';
        import SimpleCard from '@components/SimpleCard.astro';

        # Arc

        Arc is the application layer.

        ```csharp
        import legacy from 'not-an-mdx-import';
        ```
        """;

    Chunk[] _chunks = null!;

    void Because() => _chunks = [.. MarkdownChunker.Chunk(new PageUrl("https://cratis.io/arc.md"), Markdown)];

    [Fact] void should_strip_mdx_import_lines() => _chunks[0].Content.ShouldNotContain("@astrojs/starlight");
    [Fact] void should_keep_the_body() => _chunks[0].Content.ShouldContain("Arc is the application layer.");
    [Fact] void should_keep_import_looking_lines_inside_code_fences() => _chunks[0].Content.ShouldContain("not-an-mdx-import");
}
