// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Microsoft.Extensions.AI;

namespace Cratis.Prompter.Specs.for_UsagePrice.when_pricing;

public class reported_usage : Specification
{
    readonly ProviderBudget _budget = new() { InputUsdPerMillion = 4, OutputUsdPerMillion = 8, CacheWriteUsdPerMillion = 2, CacheReadUsdPerMillion = 1 };

    [Theory]
    [InlineData(null, null, 0, 0, true, SettlementRejection.NoUsage)]
    [InlineData(-1L, 0L, 0, 0, true, SettlementRejection.NoUsage)]
    [InlineData(10L, 0L, -1, 0, true, SettlementRejection.CacheCountsInvalid)]
    [InlineData(10L, 0L, 0, -1, true, SettlementRejection.CacheCountsInvalid)]
    [InlineData(10L, 0L, 11, 0, true, SettlementRejection.CacheCountsInvalid)]
    [InlineData(10L, 0L, 6, 5, true, SettlementRejection.CacheCountsInvalid)]
    [InlineData(long.MaxValue, 0L, long.MaxValue, long.MaxValue, true, SettlementRejection.CacheCountsInvalid)]
    [InlineData(10L, null, 0, 0, true, SettlementRejection.OutputInvalid)]
    [InlineData(10L, -1L, 0, 0, false, SettlementRejection.OutputInvalid)]
    [InlineData(10L, 0L, 1, 0, false, SettlementRejection.UnknownUsageCounter)]
    void should_reject_unpriceable_counts(long? input, long? output, long write, long read, bool chat, SettlementRejection rejection) =>
        UsagePrice.Of(_budget, new() { InputTokenCount = input, OutputTokenCount = output, CachedInputTokenCount = read, AdditionalCounts = new() { ["CacheCreationInputTokens"] = write } }, chat).ShouldEqual((null, rejection));

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
