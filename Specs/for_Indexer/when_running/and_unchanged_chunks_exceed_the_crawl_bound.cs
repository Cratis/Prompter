// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Indexer.when_running;

public class and_unchanged_chunks_exceed_the_crawl_bound : given.an_unending_crawl
{
    void Establish()
    {
        Options.Ingestion.MaxCrawlChunks = 2;
        var known = Enumerable.Range(1, 3)
            .SelectMany(index => MarkdownChunker.Chunk(new PageUrl($"https://docs.invalid/{index}.md"), Pages.Markdown))
            .ToDictionary(chunk => chunk.Id, chunk => chunk.Hash);
        Chunks.GetHashes(Arg.Any<CancellationToken>()).Returns(known);
    }

    Task Because() => RunUntilRejected();

    [Fact] void should_bound_retained_chunks_as_well_as_changed_ones() => Pages.Count.ShouldEqual(3);
    [Fact] void should_report_an_incomplete_crawl() => Error.ShouldBeOfExactType<IncompleteCrawl>();
    [Fact] void should_leave_the_complete_snapshot_unchanged() => NothingPublished();
}
