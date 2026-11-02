// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_Indexer.when_running;

public class and_the_crawl_exceeds_the_chunk_bound : given.an_unending_crawl
{
    void Establish() => Options.Ingestion.MaxCrawlChunks = 2;

    Task Because() => RunUntilRejected();

    [Fact] void should_stop_while_accumulating_chunks() => Pages.Count.ShouldEqual(3);
    [Fact] void should_report_an_incomplete_crawl() => Error.ShouldBeOfExactType<IncompleteCrawl>();
    [Fact] void should_leave_the_complete_snapshot_unchanged() => NothingPublished();
}
