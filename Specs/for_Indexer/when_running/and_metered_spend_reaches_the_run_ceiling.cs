// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Cratis.Prompter.Specs.Fakes;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Indexer.when_running;

public class and_metered_spend_reaches_the_run_ceiling : given.an_indexer
{
    void Establish()
    {
        Options.Voyage.BatchSize = 75;
        Embeddings.Attempts = 1;
        Embeddings.UsageFor = inputs => inputs.Sum(IndexBudget.TokensFor);
        var pages = Enumerable.Range(0, 150).Select(index => new DocsPage(new PageUrl($"https://cratis.io/{index}.md"), new string('x', 4_000))).ToArray();
        var inputs = pages.SelectMany(page => MarkdownChunker.Chunk(page.Url, page.Markdown)).Select(chunk => chunk.EmbeddingInput).ToArray();
        Options.Ingestion.MaxRunDollars = IndexBudget.Preflight(inputs, Options, new()).Dollars;
        Indexer = Create(new FakeDocsSite(pages));
    }

    async Task Because() => Error = await Catch.Exception(async () => await Indexer.Run());

    [Fact] void should_reject_the_second_batch() => Error.ShouldBeOfExactType<IndexBudgetExceeded>();
    [Fact] void should_dispatch_only_the_first_batch() => Embeddings.BatchSizes.Count.ShouldEqual(1);
    [Fact] void should_not_publish() => Lease.DidNotReceive().CommitIndex(Arg.Any<IReadOnlyCollection<EmbeddedChunk>>(), Arg.Any<IReadOnlySet<ChunkId>>(), Arg.Any<IReadOnlyCollection<DocumentationMetadata>>(), Arg.Any<CancellationToken>());
}
