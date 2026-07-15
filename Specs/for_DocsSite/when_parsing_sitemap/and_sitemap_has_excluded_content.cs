// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_DocsSite.when_parsing_sitemap;

public class and_sitemap_has_excluded_content : Specification
{
    const string Sitemap =
        """
        <urlset>
            <url><loc>https://cratis.io/chronicle/events/</loc></url>
            <url><loc>https://cratis.io/api-reference/chronicle/</loc></url>
            <url><loc>https://cratis.io/chronicle/client-snippets/append-event/</loc></url>
        </urlset>
        """;

    PageUrl[] _urls = null!;

    void Because() => _urls = [.. DocsSite.ParsePageUrls(Sitemap)];

    [Fact] void should_exclude_api_reference() => _urls.Any(url => url.Value.Contains("api-reference")).ShouldBeFalse();
    [Fact] void should_exclude_client_snippets() => _urls.Any(url => url.Value.Contains("client-snippets")).ShouldBeFalse();
    [Fact] void should_keep_documentation_pages() => _urls.ShouldContainOnly(new PageUrl("https://cratis.io/chronicle/events.md"));
}
