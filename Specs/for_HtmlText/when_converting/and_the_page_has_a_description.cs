// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_HtmlText.when_converting;

public class and_the_page_has_a_description : Specification
{
    [Theory]
    [InlineData("<meta name='description' content='A &quot;quoted&quot;\nsummary'>")]
    [InlineData("<meta content='A &quot;quoted&quot;\nsummary' name='description'>")]
    public void should_neutralize_and_quote_metadata(string meta)
    {
        var markdown = HtmlText.ToMarkdown($"<title>Cratis &amp; Studio</title>{meta}<body><p>Prose</p></body>");
        markdown.ShouldEqual("---\ntitle: \"Cratis & Studio\"\ndescription: \"A 'quoted' summary\"\n---\n# Cratis & Studio\n\nProse");
        var split = DocumentationMetadata.Split(markdown);
        DocumentationMetadata.Unique(split.Lines, "description").ShouldEqual("A 'quoted' summary");
    }
}
