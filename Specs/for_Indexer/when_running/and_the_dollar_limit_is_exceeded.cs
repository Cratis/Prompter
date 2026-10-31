// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_Indexer.when_running;

public class and_the_dollar_limit_is_exceeded : given.an_indexer
{
    void Establish() => Options.Ingestion.MaxRunDollars = 0.000001m;
    async Task Because() => Error = await Catch.Exception(async () => await Indexer.Run());

    [Fact] void should_fail_before_spending() => Error.ShouldBeOfExactType<IndexBudgetExceeded>();
    [Fact] void should_not_call_the_provider() => Embeddings.BatchSizes.Count.ShouldEqual(0);
}
