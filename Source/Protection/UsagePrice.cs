// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        if (usage.AdditionalCounts?.Any(count => count.Value != 0 && !(chat && count.Key == CacheCreation)) == true)
        {
            return (null, SettlementRejection.UnknownUsageCounter);
        }

        var input = usage.InputTokenCount.Value;
        var write = usage.AdditionalCounts is not null && usage.AdditionalCounts.TryGetValue(CacheCreation, out var created) ? created : 0;
        var read = usage.CachedInputTokenCount ?? 0;

        // Compared without adding, so provider-reported counts near long.MaxValue cannot overflow past the check.
        if (write < 0 || read < 0 || write > input || read > input - write)
        {
            return (null, SettlementRejection.CacheCountsInvalid);
        }

        var writePrice = budget.CacheWriteUsdPerMillion;
        if (write > 0 && !(writePrice > 0 && writePrice <= budget.InputUsdPerMillion))
        {
            return (null, SettlementRejection.CacheWriteUnpriced);
        }

        var readPrice = budget.CacheReadUsdPerMillion is > 0 && budget.CacheReadUsdPerMillion <= budget.InputUsdPerMillion
            ? budget.CacheReadUsdPerMillion
            : budget.InputUsdPerMillion;
        var output = usage.OutputTokenCount ?? (chat ? -1 : 0);
        if (output < 0)
        {
            return (null, SettlementRejection.OutputInvalid);
        }

        var cost = (((input - write - read) * budget.InputUsdPerMillion) + (write * writePrice) + (read * readPrice) + (output * budget.OutputUsdPerMillion)) / 1000000m;
        return (cost, null);
    }
}
