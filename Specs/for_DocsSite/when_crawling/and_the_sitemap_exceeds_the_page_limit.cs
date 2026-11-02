// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_DocsSite.when_crawling;

public class and_the_sitemap_exceeds_the_page_limit : given.a_docs_fetch
{
    void Establish()
    {
        Options.Ingestion.MaxSitemapEntries = 2;
        Respond = uri => uri.AbsolutePath == "/sitemap-0.xml"
            ? Text("<loc>https://docs.invalid/a</loc><loc>https://docs.invalid/b</loc><loc>https://elsewhere.invalid/c</loc>")
            : Text("# Page");
    }

    Task Because() => Crawl();

    [Fact] void should_fail_instead_of_truncating_the_crawl() => Error.ShouldBeOfExactType<IncompleteCrawl>();
    [Fact] void should_not_start_fetching_an_oversized_page_set() => Requests.Count.ShouldEqual(1);
}
