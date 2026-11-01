// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Prompter.Specs.for_CratisSites.when_crawling;

public class and_a_page_list_is_configured : given.a_site
{
    void Establish()
    {
        _site = new SitesOptions().Direct;
        _responses["https://cratis.direct/"] = (HttpStatusCode.OK, "text/html", "<h1>Direct</h1><p>Deployment</p>");
    }
    Task Because() => Read();
    [Fact] void should_only_request_the_configured_page() => _requests.ShouldEqual(["https://cratis.direct/"]);
    [Fact] void should_yield_the_direct_page() => _pages.Single().Markdown.ShouldContain("Deployment");
}
