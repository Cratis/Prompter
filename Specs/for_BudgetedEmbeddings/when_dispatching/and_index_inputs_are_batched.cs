// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Embeddings;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Specs.Fakes;
using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_BudgetedEmbeddings.when_dispatching;

public class and_index_inputs_are_batched : for_PaidWork.given.a_paid_work
{
    RecordingEmbeddingGenerator _transport = null!;
    IProviderAttemptStore _store = null!;
    ResilientEmbeddingGenerator _embeddings = null!;
    BudgetedEmbeddings _budgeted = null!;

    static EmbeddingGenerationOptions IndexRequest() => new()
    {
        AdditionalProperties = new() { [EmbeddingRetry.TaskKey] = "index" }
    };

    void Establish()
    {
        Options.Protection.Voyage.MaxInputTokens = 32_000;
        _transport = new();
        _store = Substitute.For<IProviderAttemptStore>();
        _store.Reserve(Arg.Any<ProviderDispatch>(), Arg.Any<Guid?>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>()).Returns(true);
        var config = Microsoft.Extensions.Options.Options.Create(Options);
        _budgeted = new(_transport, Ledger, config);
        _embeddings = new(_budgeted, config, NullLogger<ResilientEmbeddingGenerator>.Instance, new ProviderAccounting(_store, config));
    }

    [Theory]
    [InlineData(2, 20_000)]
    [InlineData(10, 31_936)]
    async Task should_allow_multiple_bounded_inputs_above_the_per_input_total(int count, int characters)
    {
        var result = await _embeddings.GenerateAsync(Enumerable.Repeat(new string('x', characters), count), IndexRequest(), TestContext.Current.CancellationToken);
        result.Count.ShouldEqual(count);
        _transport.BatchSizes.Single().ShouldEqual(count);
        await _store.Received(1).Reserve(
            Arg.Is<ProviderDispatch>(dispatch => dispatch.Task == "index" && dispatch.MaxInputTokens == (characters + 64) * count),
            null,
            0.032m * count,
            Arg.Any<CancellationToken>());
        await _store.Received(1).Complete(Arg.Any<ProviderDispatch>(), null, Arg.Any<CancellationToken>());
        Ledger.ReceivedCalls().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(1, 31_937)]
    [InlineData(11, 31_936)]
    [InlineData(1001, 1)]
    async Task should_refuse_an_oversized_input_request_or_item_count(int count, int characters)
    {
        var error = await Catch.Exception(() => _embeddings.GenerateAsync(Enumerable.Repeat(new string('x', characters), count), IndexRequest()));
        error.ShouldNotBeNull();
        _transport.BatchSizes.ShouldBeEmpty();
    }

    [Fact]
    async Task should_reject_one_oversized_input_even_inside_a_small_request()
    {
        var error = await Catch.Exception(() => _embeddings.GenerateAsync([new string('x', 32_000), "small"], IndexRequest()));
        error.ShouldBeOfExactType<WorkRefused>();
        _transport.BatchSizes.ShouldBeEmpty();
    }

    [Fact]
    async Task should_preserve_the_query_aggregate_ceiling()
    {
        var error = await Catch.Exception(() => _embeddings.GenerateAsync([new string('x', 20_000), new string('x', 20_000)]));
        error.ShouldNotBeNull();
        _transport.BatchSizes.ShouldBeEmpty();
    }

    void Destroy()
    {
        _embeddings.Dispose();
        _budgeted.Dispose();
    }
}
