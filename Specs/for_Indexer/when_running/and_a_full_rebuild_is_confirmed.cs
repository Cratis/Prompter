// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Indexer.when_running;

public class and_a_full_rebuild_is_confirmed : given.an_indexer
{
    IndexRun _run = null!;
    void Establish()
    {
        Options.Ingestion.MaxIncrementalTokens = 1;
        var page = new DocsPage(new PageUrl("https://cratis.io/a.md"), "# A\n\nAlpha.");
        Chunks.GetHashes(Arg.Any<CancellationToken>()).Returns(MarkdownChunker.Chunk(page.Url, page.Markdown).ToDictionary(chunk => chunk.Id, chunk => chunk.Hash));
    }

    async Task Because() => _run = await Indexer.Run(new IndexRequest(true, "FULL REINDEX"));

    [Fact] void should_embed_unchanged_content_too() => _run.Embedded.ShouldEqual(2);
    [Fact] void should_publish_once() => Lease.Received(1).CommitIndex(Arg.Is<IReadOnlyCollection<EmbeddedChunk>>(items => items.Count == 2), Arg.Any<IReadOnlySet<ChunkId>>(), Arg.Any<IReadOnlyCollection<DocumentationMetadata>>(), Arg.Any<CancellationToken>());
}
