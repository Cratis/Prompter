// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Protection;

/// <summary>Why a provider attempt's usage could not be settled at a known cost, so its bound is retained.</summary>
public enum SettlementRejection
{
    /// <summary>The provider reported no usable input token count.</summary>
    NoUsage = 0,

    /// <summary>The usage carries a nonzero counter that has no verified price.</summary>
    UnknownUsageCounter = 1,

    /// <summary>The cache read and write counts are negative or exceed the input count.</summary>
    CacheCountsInvalid = 2,

    /// <summary>Cache-write tokens were used but no valid cache-write price (at most the input price) is configured.</summary>
    CacheWriteUnpriced = 3,

    /// <summary>The input count exceeds the verified input ceiling.</summary>
    InputAboveCeiling = 4,

    /// <summary>The output count is missing, negative or above the dispatch's maximum.</summary>
    OutputInvalid = 5,

    /// <summary>The reported total does not equal input plus output.</summary>
    TotalMismatch = 6,

    /// <summary>The computed cost exceeds the reserved upper bound.</summary>
    CostAboveUpper = 7
}
