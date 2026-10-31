// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Cratis.Prompter.Protection;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ProtectedIndexer.when_running;

public class and_a_full_rebuild_is_confirmed : for_Indexer.given.an_indexer
{
    IndexRun _run = null!;

    void Establish() => Options.Ingestion.MaxIncrementalTokens = 1;

    async Task Because()
    {
        var indexer = new ProtectedIndexer(Indexer, Microsoft.Extensions.Options.Options.Create(Options));
        _run = await indexer.Run(new IndexRequest(true, "FULL REINDEX"));
    }

    [Fact] void should_forward_full_mode() => Lease.Received(1).CheckProfile(Arg.Any<string>(), true, Arg.Any<CancellationToken>());
    [Fact] void should_use_the_full_run_budget() => _run.Embedded.ShouldEqual(2);
    [Fact] void should_mark_every_batch_as_index_work() => Embeddings.Tasks.Count(task => task == "index").ShouldEqual(2);
}
