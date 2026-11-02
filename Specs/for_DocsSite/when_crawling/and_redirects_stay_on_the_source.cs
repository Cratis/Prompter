// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_DocsSite.when_crawling;

public class and_redirects_stay_on_the_source : given.a_docs_fetch
{
    void Establish() => Respond = uri => uri.AbsolutePath switch
    {
        "/sitemap-0.xml" => Redirect("https://www.docs.invalid/map.xml"),
        "/map.xml" => Text("<loc>https://www.docs.invalid/a</loc>"),
        "/a.md" => Redirect("/mirror.md"),
        _ => Text("# A")
    };

    Task Because() => Crawl();

    [Fact] void should_follow_one_validated_hop_per_resource() => Error.ShouldBeNull();
    [Fact] void should_read_the_final_markdown() => Pages.Single().Markdown.ShouldEqual("# A");
    [Fact] void should_resolve_relative_locations_on_the_current_host() => Requests[^1].AbsoluteUri.ShouldEqual("https://www.docs.invalid/mirror.md");
}
