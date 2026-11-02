// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_CorpusSources.when_enumerating;

public class and_auxiliary_pages_exhaust_the_shared_budget : given.a_composed_indexer
{
    [Theory]
    [InlineData("pages", 2)]
    [InlineData("bytes", 1)]
    [InlineData("chunks", 1)]
    public async Task should_stop_before_buffering_more_pages_and_preserve_the_entire_snapshot(string limit, int expectedReads)
    {
        if (limit == "pages") Options.Ingestion.MaxCrawlPages = 2;
        if (limit == "bytes") Options.Ingestion.MaxCrawlBytes = DocumentationBytes + AuxiliaryBytes - 1;
        if (limit == "chunks") Options.Ingestion.MaxCrawlChunks = 2;
        Compose();
        var error = await Catch.Exception(() => Indexer.Run(TestContext.Current.CancellationToken));
        (error is IncompleteCrawl).ShouldBeTrue();
        AuxiliaryReads.ShouldEqual(expectedReads);
        Corpus.FailedSources.ShouldBeEmpty();
        Embeddings.BatchSizes.ShouldBeEmpty();
        Lease.ReceivedCalls().Where(call => call.GetMethodInfo().Name == "CommitIndex").ShouldBeEmpty();
    }
}
