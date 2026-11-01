// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Prompter.Specs.for_CratisSites.when_crawling;

public class and_the_sitemap_lists_another_host : given.a_site
{
    void Establish()
    {
        _responses["https://cratis.no/sitemap.xml"] = (HttpStatusCode.OK, "application/xml", "<loc>https://evil.invalid/x</loc><loc>https://cratis.no.evil.invalid/</loc><loc>https://cratis.no/about/</loc>");
        _responses["https://cratis.no/about/"] = (HttpStatusCode.OK, "text/html", "<main>Company</main>");
    }
    Task Because() => Read();
    [Fact] void should_fetch_only_the_sitemap_and_owned_page() => _requests.ShouldEqual(["https://cratis.no/sitemap.xml", "https://cratis.no/about/"]);
    [Fact] void should_yield_only_the_owned_page() => _pages.Single().Url.Value.ShouldEqual("https://cratis.no/about/");
    [Fact] void should_complete() => _error.ShouldBeNull();
}
