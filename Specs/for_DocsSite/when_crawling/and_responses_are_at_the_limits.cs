// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_DocsSite.when_crawling;

public class and_responses_are_at_the_limits : given.a_docs_fetch
{
    void Establish()
    {
        var sitemap = string.Concat(Enumerable.Range(0, 10000).Select(index => $"<loc>https://docs.invalid/{index}</loc>"));
        Respond = uri => uri.AbsolutePath switch
        {
            "/sitemap-0.xml" => Text(sitemap.PadRight(8 * 1024 * 1024)),
            "/0.md" => Text(new string('a', 1024 * 1024)),
            _ => Text("# Page")
        };
    }

    Task Because() => Crawl();

    [Fact] void should_accept_exact_sitemap_page_and_count_boundaries() => Error.ShouldBeNull();
    [Fact] void should_not_truncate_the_page_set() => Pages.Count.ShouldEqual(10000);
}
