// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_MarkdownChunker.when_chunking;

public class and_a_heading_differs_from_the_frontmatter_title : Specification
{
    const string Markdown = "---\ntitle: Short title\n---\n\n# The page heading\n\nInstall the packages.";

    Chunk[] _chunks = null!;

    void Because() => _chunks = [.. MarkdownChunker.Chunk(new PageUrl("https://cratis.io/getting-started.md"), Markdown)];

    [Fact] void should_keep_the_level_one_heading() => _chunks.Single().Title.ShouldEqual("The page heading");
}
