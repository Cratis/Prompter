// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Cratis.Prompter.Specs.Fakes;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Indexer.when_running;

public class and_the_crawl_is_exactly_at_the_bounds : given.an_indexer
{
    IndexRun _run = null!;

    void Establish()
    {
        Options.Ingestion.MaxCrawlPages = 2;
        Options.Ingestion.MaxCrawlBytes = 18;
        Options.Ingestion.MaxCrawlChunks = 2;
        Indexer = Create(new FakeDocsSite([
            new(new PageUrl("https://docs.invalid/a.md"), "# A\n\néé"),
            new(new PageUrl("https://docs.invalid/b.md"), "# B\n\néé")
        ]));
    }

    async Task Because() => _run = await Indexer.Run();

    [Fact] void should_accept_the_complete_crawl() => _run.Pages.ShouldEqual(2);
    [Fact] void should_embed_both_candidates() => _run.Embedded.ShouldEqual(2);
    [Fact] void should_publish_the_snapshot() => Lease.Received(1).CommitIndex(Arg.Any<IReadOnlyCollection<EmbeddedChunk>>(), Arg.Any<IReadOnlySet<ChunkId>>(), Arg.Any<IReadOnlyCollection<DocumentationMetadata>>(), Arg.Any<CancellationToken>());
}
