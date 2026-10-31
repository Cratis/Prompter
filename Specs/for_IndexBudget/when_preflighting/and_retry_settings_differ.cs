// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Embeddings;
using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_IndexBudget.when_preflighting;

public class and_retry_settings_differ : Specification
{
    static PrompterOptions Config() => new()
    {
        Ingestion = { PricePerMillionTokens = 1, MaxRunDollars = 10, MaxDailyDollars = 10 }
    };

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, 1)]
    [InlineData(2, 3)]
    [InlineData(5, 6)]
    [InlineData(99, 6)]
    void should_bound_the_same_attempts_as_the_index_transport(int retries, int attempts)
    {
        var options = Config();
        options.Voyage.IndexMaxRetries = retries;
        var exposure = IndexBudget.Preflight(["é"], options, new());
        exposure.Tokens.ShouldEqual(66L * attempts);
        exposure.Dollars.ShouldEqual(0.000066m * attempts);
        EmbeddingRetryPolicy.Attempts(options.Voyage, indexing: true).ShouldEqual(attempts);
    }

    [Fact]
    void should_use_the_index_default_instead_of_the_query_default()
    {
        var options = Config();
        var attempts = EmbeddingRetryPolicy.Attempts(options.Voyage, indexing: true);
        IndexBudget.Preflight(["é"], options, new()).Tokens.ShouldEqual(66L * attempts);
        attempts.ShouldEqual(6);
    }

    [Fact]
    void should_reject_a_budget_that_only_funds_query_retries()
    {
        var options = Config();
        options.Voyage.MaxRetries = 0;
        options.Ingestion.MaxRunDollars = 0.0001m;
        Catch.Exception(() => IndexBudget.Preflight(["é"], options, new())).ShouldBeOfExactType<IndexBudgetExceeded>();
    }

    [Theory]
    [InlineData(31_936, false)]
    [InlineData(31_937, true)]
    void should_include_framing_in_the_per_input_limit(int characters, bool rejected)
    {
        var error = Catch.Exception(() => IndexBudget.Preflight([new string('x', characters)], Config(), new()));
        (error is IndexBudgetExceeded).ShouldEqual(rejected);
    }
}
