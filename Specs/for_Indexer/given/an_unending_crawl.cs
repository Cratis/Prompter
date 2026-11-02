// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Prompter.Ingestion;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Indexer.given;

public class an_unending_crawl : an_indexer
{
    protected EndlessPages Pages { get; } = new();

    protected async Task RunUntilRejected(IndexRequest? request = null)
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        Error = await Catch.Exception(async () => await Indexer.Run(request ?? new IndexRequest(), cancellation.Token));
    }

    protected void NothingPublished()
    {
        Embeddings.BatchSizes.ShouldBeEmpty();
        Lease.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(IIndexLease.CommitIndex)).ShouldBeFalse();
    }

    void Establish() => Indexer = Create(Pages);

    protected sealed class EndlessPages : IDocsSite
    {
        public int Count { get; private set; }
        public string Markdown { get; set; } = "# A\n\nAlpha.";

        public async IAsyncEnumerable<DocsPage> GetPages([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Count++;
                yield return new(new PageUrl($"https://docs.invalid/{Count}.md"), Markdown);
                await Task.Yield();
            }
        }
    }
}
