// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_DocsSite.when_parsing_sitemap;

public class and_sitemap_has_pages : Specification
{
    const string Sitemap =
        """
        <?xml version="1.0" encoding="UTF-8"?>
        <urlset>
            <url><loc>https://cratis.io/</loc></url>
            <url><loc>https://cratis.io/chronicle/getting-started/</loc></url>
        </urlset>
        """;

    PageUrl[] _urls = null!;

    void Because() => _urls = [.. DocsSite.ParsePageUrls(Sitemap)];

    [Fact] void should_map_pages_to_their_markdown_mirror() => _urls.ShouldContain(new PageUrl("https://cratis.io/chronicle/getting-started.md"));
    [Fact] void should_map_the_root_page_to_index() => _urls.ShouldContain(new PageUrl("https://cratis.io/index.md"));
    [Fact] void should_include_all_pages() => _urls.Length.ShouldEqual(2);
}
