// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Microsoft.Extensions.AI;

namespace Cratis.Prompter.Specs.for_UsagePrice.when_pricing;

public class reported_usage : Specification
{
    readonly ProviderBudget _budget = new() { InputUsdPerMillion = 4, OutputUsdPerMillion = 8, CacheWriteUsdPerMillion = 2, CacheReadUsdPerMillion = 1 };

    [Theory]
    [InlineData(null, null)]
    [InlineData(-1L, 0L)]
    void should_reject_missing_or_negative_input(long? input, long? output) =>
        UsagePrice.Of(_budget, new() { InputTokenCount = input, OutputTokenCount = output }, true).ShouldEqual((null, SettlementRejection.NoUsage));

    [Theory]
    [InlineData(10, -1, 0)]
    [InlineData(10, 0, -1)]
    [InlineData(10, 11, 0)]
    [InlineData(10, 6, 5)]
    [InlineData(long.MaxValue, long.MaxValue, long.MaxValue)]
    void should_reject_invalid_cache_counts(long input, long write, long read) =>
        UsagePrice.Of(_budget, new() { InputTokenCount = input, OutputTokenCount = 0, CachedInputTokenCount = read, AdditionalCounts = new() { ["CacheCreationInputTokens"] = write } }, true).ShouldEqual((null, SettlementRejection.CacheCountsInvalid));

    [Fact] void should_reject_chat_usage_without_output() => UsagePrice.Of(_budget, new() { InputTokenCount = 10 }, true).ShouldEqual((null, SettlementRejection.OutputInvalid));
    [Fact] void should_reject_negative_embedding_output() => UsagePrice.Of(_budget, new() { InputTokenCount = 10, OutputTokenCount = -1 }, false).ShouldEqual((null, SettlementRejection.OutputInvalid));
    [Fact] void should_reject_embedding_cache_writes() => UsagePrice.Of(_budget, new() { InputTokenCount = 10, OutputTokenCount = 0, AdditionalCounts = new() { ["CacheCreationInputTokens"] = 1 } }, false).ShouldEqual((null, SettlementRejection.UnknownUsageCounter));

    [Fact] void should_reject_absent_usage() => UsagePrice.Of(_budget, null, true).Rejection.ShouldEqual(SettlementRejection.NoUsage);
    [Fact] void should_allow_zero_unknown_counters() => UsagePrice.Of(_budget, new() { InputTokenCount = 10, OutputTokenCount = 0, AdditionalCounts = new() { ["unknown"] = 0 } }, true).Cost.ShouldEqual(0.00004m);
    [Fact] void should_reject_nonzero_unknown_counters() => UsagePrice.Of(_budget, new() { InputTokenCount = 10, OutputTokenCount = 0, AdditionalCounts = new() { ["unknown"] = 1 } }, true).Rejection.ShouldEqual(SettlementRejection.UnknownUsageCounter);
    [Fact] void should_allow_embedding_usage_without_output() => UsagePrice.Of(_budget, new() { InputTokenCount = 10 }, false).Cost.ShouldEqual(0.00004m);
    [Fact] void should_price_each_token_category() => UsagePrice.Of(_budget, new() { InputTokenCount = 10, OutputTokenCount = 3, CachedInputTokenCount = 2, AdditionalCounts = new() { ["CacheCreationInputTokens"] = 4 } }, true).Cost.ShouldEqual(0.00005m);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(5)]
    void should_reject_invalid_write_prices(int price)
    {
        _budget.CacheWriteUsdPerMillion = price;
        UsagePrice.Of(_budget, new() { InputTokenCount = 10, OutputTokenCount = 0, AdditionalCounts = new() { ["CacheCreationInputTokens"] = 1 } }, true).Rejection.ShouldEqual(SettlementRejection.CacheWriteUnpriced);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(5)]
    void should_fall_back_to_the_input_price_for_reads(int price)
    {
        _budget.CacheReadUsdPerMillion = price;
        UsagePrice.Of(_budget, new() { InputTokenCount = 10, OutputTokenCount = 0, CachedInputTokenCount = 10 }, true).Cost.ShouldEqual(0.00004m);
    }
}
