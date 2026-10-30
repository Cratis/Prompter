// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Embeddings;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ProviderAccounting.when_dispatching;

public class indexing_is_funded_once : given.durable_accounting
{
    IEmbeddingGenerator<string, Embedding<float>> _provider = null!;

    async Task Because()
    {
        _provider = Substitute.For<IEmbeddingGenerator<string, Embedding<float>>>();
        _provider.GenerateAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<EmbeddingGenerationOptions?>(), Arg.Any<CancellationToken>())
            .Returns(new GeneratedEmbeddings<Embedding<float>>([new(new float[Options.Voyage.Dimensions])]) { Usage = new() { InputTokenCount = 1, TotalTokenCount = 1 } });
        using var bounded = new BudgetedEmbeddings(_provider, Ledger, Microsoft.Extensions.Options.Options.Create(Options));
        using var resilient = new ResilientEmbeddingGenerator(bounded, Microsoft.Extensions.Options.Options.Create(Options), NullLogger<ResilientEmbeddingGenerator>.Instance, Accounting);
        await resilient.GenerateAsync(["document"], new() { AdditionalProperties = new() { [EmbeddingRetry.TaskKey] = "index" } });
    }

    [Fact] void should_reserve_independently_through_accounting() => Store.Received(1).Reserve(Arg.Is<ProviderDispatch>(dispatch => dispatch.Provider == "voyage" && dispatch.Task == "index"), null, Arg.Any<decimal>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_reserve_again_in_the_inner_decorator() => Ledger.DidNotReceive().Reserve(Arg.Any<SpendExposure>(), Arg.Any<CancellationToken>());
    [Fact] void should_dispatch_one_request() => _provider.Received(1).GenerateAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<EmbeddingGenerationOptions?>(), Arg.Any<CancellationToken>());
}
