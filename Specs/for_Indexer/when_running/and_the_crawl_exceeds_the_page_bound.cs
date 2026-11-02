// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_Indexer.when_running;

public class and_the_crawl_exceeds_the_page_bound : given.an_unending_crawl
{
    void Establish() => Options.Ingestion.MaxCrawlPages = 2;

    Task Because() => RunUntilRejected();

    [Fact] void should_stop_on_the_first_excess_page() => Pages.Count.ShouldEqual(3);
    [Fact] void should_report_an_incomplete_crawl() => Error.ShouldBeOfExactType<IncompleteCrawl>();
    [Fact] void should_leave_the_complete_snapshot_unchanged() => NothingPublished();
}
