// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Prompter.Specs.for_CratisSites.when_crawling;

public class and_the_page_cap_is_reached : given.a_site
{
    void Establish()
    {
        _site.Sitemap = string.Empty;
        _site.Pages = ["/", "/", "/about/"];
        _site.LlmsTxt = true;
        _site.MaxPages = 1;
        _responses["https://cratis.no/"] = (HttpStatusCode.OK, "text/html", "<h1>Company</h1>");
    }
    Task Because() => Read();
    [Fact] void should_take_configured_pages_before_llms_and_deduplicate() => _requests.ShouldEqual(["https://cratis.no/"]);
    [Fact] void should_yield_one_page() => _pages.Count.ShouldEqual(1);
}
