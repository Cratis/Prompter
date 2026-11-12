// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Microsoft.Extensions.AI;

namespace Cratis.Prompter.Protection;

/// <summary>Prices reported provider usage, never above the input-price bound that sizes the reservation.</summary>
internal static class UsagePrice
{
    const string CacheCreation = "CacheCreationInputTokens";

    /// <summary>Prices usage at the verified rates.</summary>
    /// <param name="budget">The verified prices.</param>
    /// <param name="usage">The reported usage.</param>
    /// <param name="chat">Whether the usage is chat usage, which requires an output count.</param>
    /// <returns>The cost, or the reason the usage cannot be priced.</returns>
    internal static (decimal? Cost, SettlementRejection? Rejection) Of(ProviderBudget budget, UsageDetails? usage, bool chat)
    {
        if (usage?.InputTokenCount is not >= 0)
        {
            return (null, SettlementRejection.NoUsage);
        }

        if (HasUnknownCounts(usage, chat))
        {
            return (null, SettlementRejection.UnknownUsageCounter);
        }

        var input = usage.InputTokenCount.Value;
        var write = usage.AdditionalCounts is not null && usage.AdditionalCounts.TryGetValue(CacheCreation, out var created) ? created : 0;
        var read = usage.CachedInputTokenCount ?? 0;
        if (InvalidCacheCounts(input, write, read))
        {
            return (null, SettlementRejection.CacheCountsInvalid);
        }

        if (write > 0 && !ValidCachePrice(budget.CacheWriteUsdPerMillion, budget.InputUsdPerMillion))
        {
            return (null, SettlementRejection.CacheWriteUnpriced);
        }

        var output = usage.OutputTokenCount ?? (chat ? -1 : 0);
        if (output < 0)
        {
            return (null, SettlementRejection.OutputInvalid);
        }

        return (Price(budget, new(input, output, write, read)), null);
    }

    static bool HasUnknownCounts(UsageDetails usage, bool chat) =>
        usage.AdditionalCounts?.Any(count => count.Value != 0 && !(chat && count.Key == CacheCreation)) == true;

    /// <summary>Compares without adding, so provider-reported counts near long.MaxValue cannot overflow past the check.</summary>
    /// <param name="input">The reported input tokens.</param>
    /// <param name="write">The reported cache-write tokens.</param>
    /// <param name="read">The reported cache-read tokens.</param>
    /// <returns>Whether the cache counts are invalid.</returns>
    static bool InvalidCacheCounts(long input, long write, long read) =>
        write < 0 || read < 0 || write > input || read > input - write;

    static bool ValidCachePrice(decimal price, decimal inputPrice) => price > 0 && price <= inputPrice;

    static decimal Price(ProviderBudget budget, TokenCounts counts)
    {
        var readPrice = ValidCachePrice(budget.CacheReadUsdPerMillion, budget.InputUsdPerMillion)
            ? budget.CacheReadUsdPerMillion
            : budget.InputUsdPerMillion;
        return (((counts.Input - counts.Write - counts.Read) * budget.InputUsdPerMillion) + (counts.Write * budget.CacheWriteUsdPerMillion) + (counts.Read * readPrice) + (counts.Output * budget.OutputUsdPerMillion)) / 1000000m;
    }

    [StructLayout(LayoutKind.Auto)]
    readonly record struct TokenCounts(long Input, long Output, long Write, long Read);
}
