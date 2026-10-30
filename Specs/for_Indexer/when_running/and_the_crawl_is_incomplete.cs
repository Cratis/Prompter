// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Prompter.Ingestion;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Indexer.when_running;

public class and_the_crawl_is_incomplete : given.an_indexer
{
    void Establish() => Indexer = Create(new BrokenSite());
    async Task Because() => Error = await Catch.Exception(async () => await Indexer.Run());

    [Fact] void should_fail_the_entire_pass() => Error.ShouldBeOfExactType<IncompleteCrawl>();
    [Fact] void should_not_dispatch_even_the_first_page() => Embeddings.BatchSizes.Count.ShouldEqual(0);
    [Fact] void should_not_publish_or_delete() => Lease.DidNotReceive().CommitIndex(Arg.Any<IReadOnlyCollection<EmbeddedChunk>>(), Arg.Any<IReadOnlySet<ChunkId>>(), Arg.Any<CancellationToken>());

    sealed class BrokenSite : IDocsSite
    {
        public async IAsyncEnumerable<DocsPage> GetPages([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new(new PageUrl("https://cratis.io/a.md"), "# A\n\nAlpha.");
            await Task.CompletedTask;
            throw new IncompleteCrawl();
        }
    }
}
