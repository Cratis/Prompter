// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_DocsSite.when_crawling;

public class and_a_second_redirect_is_returned : given.a_docs_fetch
{
    void Establish() => Respond = _ => Redirect("/next.xml");

    Task Because() => Crawl();

    [Fact] void should_fail_the_crawl() => Error.ShouldBeOfExactType<IncompleteCrawl>();
    [Fact] void should_stop_after_two_requests() => Requests.Count.ShouldEqual(2);
}
