// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_Indexer.when_running;

public class and_pricing_is_unconfigured : given.an_indexer
{
    void Establish() => Options.Ingestion.PricePerMillionTokens = 0;
    async Task Because() => Error = await Catch.Exception(async () => await Indexer.Run());

    [Fact] void should_fail_closed() => Error.ShouldBeOfExactType<IndexBudgetExceeded>();
    [Fact] void should_not_call_the_provider() => Embeddings.BatchSizes.Count.ShouldEqual(0);
}
