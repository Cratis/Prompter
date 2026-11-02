// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_DocsSite.when_crawling;

public class and_the_sitemap_has_a_malformed_url : given.a_docs_fetch
{
    void Establish() => Respond = _ => Text("<loc>https://[</loc>");

    Task Because() => Crawl();

    [Fact] void should_report_an_incomplete_crawl() => Error.ShouldBeOfExactType<IncompleteCrawl>();
    [Fact] void should_not_issue_a_page_request() => Requests.Count.ShouldEqual(1);
}
