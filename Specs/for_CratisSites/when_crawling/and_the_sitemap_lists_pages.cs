// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Prompter.Specs.for_CratisSites.when_crawling;

public class and_the_sitemap_lists_pages : given.a_site
{
    void Establish()
    {
        _site.ExcludedPaths = ["/privacy-policy"];
        _responses["https://cratis.no/sitemap.xml"] = (HttpStatusCode.OK, "text/xml", "<loc>https://cratis.no/</loc><loc>https://cratis.no/about/</loc><loc>https://cratis.no/privacy-policy</loc><loc>https://cratis.no/about/</loc><loc>https://cratis.no/?x=1</loc><loc>https://cratis.no/#x</loc>");
        _responses["https://cratis.no/"] = (HttpStatusCode.OK, "text/html", "<h1>Company</h1>");
        _responses["https://cratis.no/about/"] = (HttpStatusCode.OK, "text/html", "<h1>About</h1>");
    }
    Task Because() => Read();
    [Fact] void should_deduplicate_and_exclude_legal_and_query_pages() => _requests.ShouldEqual(["https://cratis.no/sitemap.xml", "https://cratis.no/", "https://cratis.no/about/"]);
    [Fact] void should_keep_canonical_urls() => _pages.Select(page => page.Url.Value).ShouldEqual(["https://cratis.no/", "https://cratis.no/about/"]);
    [Fact] void should_convert_html_to_markdown() => _pages[0].Markdown.ShouldEqual("# Company");
}
