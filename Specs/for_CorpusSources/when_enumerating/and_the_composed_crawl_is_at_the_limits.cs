// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Cratis.Prompter.Storage;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_CorpusSources.when_enumerating;

public class and_the_composed_crawl_is_at_the_limits : given.a_composed_indexer
{
    IndexRun _run = null!;

    void Establish()
    {
        Options.Ingestion.MaxCrawlPages = 4;
        Options.Ingestion.MaxCrawlBytes = DocumentationBytes + (3 * AuxiliaryBytes);
        Options.Ingestion.MaxCrawlChunks = 7;
        Compose();
    }

    async Task Because() => _run = await Indexer.Run();

    [Fact] void should_read_every_auxiliary_page_once() => AuxiliaryReads.ShouldEqual(3);
    [Fact] void should_count_documentation_and_auxiliary_pages_together() => _run.Pages.ShouldEqual(4);
    [Fact] void should_count_metadata_summary_chunks_in_the_output() => _run.Embedded.ShouldEqual(7);
    [Fact]
    void should_publish_all_page_metadata() => Lease.Received(1).CommitIndex(
        Arg.Any<IReadOnlyCollection<EmbeddedChunk>>(),
        Arg.Any<IReadOnlySet<ChunkId>>(),
        Arg.Is<IReadOnlyCollection<DocumentationMetadata>>(metadata => metadata.Count == 4 && metadata.Count(page => page.ProductVersion == "1.2.3") == 3),
        Arg.Any<CancellationToken>());
}
