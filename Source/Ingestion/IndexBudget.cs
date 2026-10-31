// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Embeddings;

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// Preflights byte-level token exposure, including the configured provider retry ceiling.
/// </summary>
public static class IndexBudget
{
    /// <summary>
    /// Reserves one token per UTF-8 byte plus input framing, rather than assuming characters/4.
    /// </summary>
    /// <param name="input">The exact embedding input.</param>
    /// <returns>The conservative input token bound.</returns>
    public static long TokensFor(string input) => EmbeddingInputLimits.TokensFor(input);

    /// <summary>
    /// Rejects unpriced or over-budget work before any embedding request.
    /// </summary>
    /// <param name="inputs">All changed inputs in the complete crawl.</param>
    /// <param name="options">The ingestion and retry configuration.</param>
    /// <param name="request">The authorized run mode.</param>
    /// <returns>The retry-inclusive token and dollar reservation.</returns>
    /// <exception cref="IndexBudgetExceeded">The configured price or ceilings do not permit this run.</exception>
    public static IndexExposure Preflight(IEnumerable<string> inputs, PrompterOptions options, IndexRequest request)
    {
        var bounds = inputs.Select(TokensFor).ToArray();
        if (bounds.Any(tokens => tokens > EmbeddingInputLimits.PerInput(options.Protection.Voyage.MaxInputTokens))) throw new IndexBudgetExceeded();
        var tokens = checked(bounds.Sum() * EmbeddingRetryPolicy.Attempts(options.Voyage, indexing: true));
        var limits = options.Ingestion;
        var cap = request.Full ? limits.MaxFullTokens : limits.MaxIncrementalTokens;
        var dollars = tokens * limits.PricePerMillionTokens / 1_000_000m;
        if (tokens > cap) throw new IndexBudgetExceeded();
        if (tokens > 0) ValidatePaidWork(limits, dollars);
        return new(tokens, dollars);
    }

    static void ValidatePaidWork(IngestionOptions limits, decimal dollars)
    {
        var disabled = new[] { limits.PricePerMillionTokens, limits.MaxRunDollars, limits.MaxDailyDollars }.Any(value => value <= 0);
        if (disabled) throw new IndexBudgetExceeded();
        if (dollars > limits.MaxRunDollars) throw new IndexBudgetExceeded();
    }
}
