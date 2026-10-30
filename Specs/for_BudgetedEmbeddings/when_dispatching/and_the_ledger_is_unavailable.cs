// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Microsoft.Extensions.AI;
using Npgsql;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_BudgetedEmbeddings.when_dispatching;

public class and_the_ledger_is_unavailable : for_PaidWork.given.a_paid_work
{
    IEmbeddingGenerator<string, Embedding<float>> _provider = null!;
    Exception? _error;

    void Establish()
    {
        _provider = Substitute.For<IEmbeddingGenerator<string, Embedding<float>>>();
        Ledger.Reserve(Arg.Any<SpendExposure>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<Guid?>(new NpgsqlException("Synthetic unavailable ledger")));
    }

    async Task Because()
    {
        using var embeddings = new BudgetedEmbeddings(_provider, Ledger, Microsoft.Extensions.Options.Options.Create(Options));
        _error = await Catch.Exception(() => embeddings.GenerateAsync(["hello"]));
    }

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<WorkRefused>();
    [Fact] void should_not_dispatch_to_voyage() =>
        _provider.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(_provider.GenerateAsync)).ShouldEqual(0);
}
