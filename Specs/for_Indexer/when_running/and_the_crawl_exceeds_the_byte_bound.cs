// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_Indexer.when_running;

public class and_the_crawl_exceeds_the_byte_bound : given.an_unending_crawl
{
    void Establish()
    {
        Pages.Markdown = "# A\n\néé";
        Options.Ingestion.MaxCrawlBytes = 18;
    }

    Task Because() => RunUntilRejected();

    [Fact] void should_count_utf8_bytes_instead_of_characters() => Pages.Count.ShouldEqual(3);
    [Fact] void should_report_an_incomplete_crawl() => Error.ShouldBeOfExactType<IncompleteCrawl>();
    [Fact] void should_leave_the_complete_snapshot_unchanged() => NothingPublished();
}
