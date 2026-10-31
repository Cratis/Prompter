// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Cratis.Prompter.Specs.Fakes;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Indexer.given;

public class an_indexer : Specification
{
    protected IChunks Chunks { get; private set; } = null!;
    protected IIndexLease Lease { get; private set; } = null!;
    protected RecordingEmbeddingGenerator Embeddings { get; private set; } = null!;
    protected PrompterOptions Options { get; private set; } = null!;
    protected Indexer Indexer { get; set; } = null!;
    protected Exception? Error { get; set; }

    protected Indexer Create(IDocsSite site) => new(site, Chunks, Embeddings, Microsoft.Extensions.Options.Options.Create(Options), NullLogger<Indexer>.Instance);

    void Establish()
    {
        Chunks = Substitute.For<IChunks>();
        Lease = Substitute.For<IIndexLease>();
        Chunks.AcquireIndexLease(Arg.Any<CancellationToken>()).Returns(Lease);
        Chunks.GetHashes(Arg.Any<CancellationToken>()).Returns(new Dictionary<ChunkId, ContentHash>());
        Embeddings = new RecordingEmbeddingGenerator();
        Options = new PrompterOptions
        {
            Voyage = { BatchSize = 1, MaxRetries = 0 },
            Ingestion = { PricePerMillionTokens = 1, MaxRunDollars = 10, MaxDailyDollars = 10 }
        };
        Indexer = Create(new FakeDocsSite([
            new(new PageUrl("https://cratis.io/a.md"), "# A\n\nAlpha."),
            new(new PageUrl("https://cratis.io/b.md"), "# B\n\nBeta.")
        ]));
    }
}
