// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Indexer.when_running;

public class and_measured_usage_is_released : given.an_indexer
{
    void Establish()
    {
        Options.Voyage.BatchSize = 1;
        Embeddings.Attempts = 1;
        Embeddings.UsageFor = inputs => 1;
        Embeddings.BeforeGenerate = batch => Lease.Received(batch).Reserve(Arg.Is<IndexReservation>(reservation => reservation.DailyLimit == 10 && !reservation.Scheduled && !reservation.Full), Arg.Any<CancellationToken>());
    }

    async Task Because() => await Indexer.Run();

    [Fact]
    async Task should_release_each_successful_batch_surplus()
    {
        var chunks = MarkdownChunker.Chunk(new PageUrl("https://cratis.io/a.md"), "# A\n\nAlpha.")
            .Concat(MarkdownChunker.Chunk(new PageUrl("https://cratis.io/b.md"), "# B\n\nBeta."));
        await Lease.Received(2).Release(Arg.Any<IndexExposure>(), Arg.Any<CancellationToken>());
        foreach (var chunk in chunks)
        {
            var reserved = IndexBudget.Batch([chunk.EmbeddingInput], Options);
            await Lease.Received(1).Release(new(reserved.Tokens - 1, reserved.Dollars - 0.000001m), Arg.Any<CancellationToken>());
        }
    }
}
