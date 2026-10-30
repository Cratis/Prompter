// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Indexer.when_running;

public class and_the_profile_is_incompatible : given.an_indexer
{
    void Establish() => Lease.CheckProfile(Arg.Any<string>(), false, Arg.Any<CancellationToken>()).Returns(_ => throw new IndexProfileMismatch());
    async Task Because() => Error = await Catch.Exception(async () => await Indexer.Run());

    [Fact] void should_require_an_approved_rebuild() => Error.ShouldBeOfExactType<IndexProfileMismatch>();
    [Fact] void should_not_embed() => Embeddings.BatchSizes.Count.ShouldEqual(0);
    [Fact] async Task should_release_the_lease() => await Lease.Received(1).DisposeAsync();
}
