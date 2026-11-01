// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_CratisSites.when_crawling;

public class and_a_page_redirects : given.a_site
{
    void Establish()
    {
        _site.Sitemap = string.Empty;
        _site.Pages = ["/"];
        _responses["https://cratis.no/"] = (HttpStatusCode.MovedPermanently, "text/html", "redirect");
    }
    Task Because() => Read();
    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<IncompleteCrawl>();
    [Fact] void should_not_fetch_the_location() => _requests.ShouldEqual(["https://cratis.no/"]);
}
