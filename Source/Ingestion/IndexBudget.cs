// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Prompter.Embeddings;

namespace Cratis.Prompter.Ingestion;

/// <summary>Estimates admission and bounds each batch's retry-inclusive exposure.</summary>
public static class IndexBudget
{
    /// <summary>Bounds input tokens using UTF-8 bytes plus framing.</summary>
    /// <param name="input">The exact embedding input.</param>
    /// <returns>The conservative token bound.</returns>
    public static long TokensFor(string input) => EmbeddingInputLimits.TokensFor(input);

    /// <summary>Estimates admission tokens; never used as a spend bound.</summary>
    /// <param name="input">The exact embedding input.</param>
    /// <returns>The estimated token count.</returns>
    public static long EstimatedTokensFor(string input) => (((long)Encoding.UTF8.GetByteCount(input) + 2) / 3) + 16;

    /// <summary>Gets the authorized run ceiling.</summary>
    /// <param name="limits">The ingestion limits.</param>
    /// <param name="request">The authorized run mode.</param>
    /// <returns>The dollar ceiling.</returns>
    public static decimal RunDollars(IngestionOptions limits, IndexRequest request) => request.Full ? limits.MaxFullRunDollars : limits.MaxRunDollars;

    /// <summary>Gets the authorized UTC-day ceiling.</summary>
    /// <param name="limits">The ingestion limits.</param>
    /// <param name="request">The authorized run mode.</param>
    /// <returns>The dollar ceiling.</returns>
    public static decimal DayDollars(IngestionOptions limits, IndexRequest request) => request.Full ? limits.MaxFullRunDollars : limits.MaxDailyDollars;

    /// <summary>Rejects unpriced or over-budget work before any embedding request.</summary>
    /// <param name="inputs">All changed inputs in the complete crawl.</param>
    /// <param name="options">The ingestion and retry configuration.</param>
    /// <param name="request">The authorized run mode.</param>
    /// <returns>The admission estimate including one batch of retry headroom.</returns>
    /// <exception cref="IndexBudgetExceeded">The configured price or ceilings do not permit this run.</exception>
    public static IndexExposure Preflight(IEnumerable<string> inputs, PrompterOptions options, IndexRequest request)
    {
        var materialized = inputs.ToArray();
        var bounds = materialized.Select(TokensFor).ToArray();
        if (bounds.Any(tokens => tokens > EmbeddingInputLimits.PerInput(options.Protection.Voyage.MaxInputTokens))) throw new IndexBudgetExceeded();
        var tokens = checked(materialized.Sum(EstimatedTokensFor) + (Math.Min(bounds.Sum(), EmbeddingInputLimits.MaximumRequestTokens) * EmbeddingRetryPolicy.Attempts(options.Voyage, indexing: true)));
        var limits = options.Ingestion;
        var cap = request.Full ? limits.MaxFullTokens : limits.MaxIncrementalTokens;
        var exposure = Price(tokens, options);
        if (tokens > cap) throw new IndexBudgetExceeded();
        if (tokens > 0)
        {
            var disabled = new[] { limits.PricePerMillionTokens, RunDollars(limits, request), DayDollars(limits, request) }.Any(value => value <= 0);
            if (disabled || exposure.Dollars > RunDollars(limits, request)) throw new IndexBudgetExceeded();
        }

        return exposure;
    }

    /// <summary>Bounds every possible attempt of a batch before dispatch.</summary>
    /// <param name="inputs">The batch inputs.</param>
    /// <param name="options">The pricing and retry configuration.</param>
    /// <returns>The retry-inclusive reservation.</returns>
    public static IndexExposure Batch(IEnumerable<string> inputs, PrompterOptions options) =>
        Price(checked(inputs.Sum(TokensFor) * EmbeddingRetryPolicy.Attempts(options.Voyage, indexing: true)), options);

    /// <summary>Counts failed attempts at their bound and a valid successful attempt at measured usage.</summary>
    /// <param name="batchBoundTokens">The one-attempt batch bound.</param>
    /// <param name="attempts">The reported attempt count.</param>
    /// <param name="usageTokens">The successful attempt's input usage.</param>
    /// <param name="options">The pricing and retry configuration.</param>
    /// <returns>Measured exposure, or the full reservation for unknown usage.</returns>
    public static IndexExposure Used(long batchBoundTokens, int attempts, long? usageTokens, PrompterOptions options)
    {
        var maximumAttempts = EmbeddingRetryPolicy.Attempts(options.Voyage, indexing: true);
        var reserved = checked(batchBoundTokens * maximumAttempts);
        var valid = attempts >= 1 && attempts <= maximumAttempts && usageTokens >= 0 && usageTokens <= batchBoundTokens;
        var tokens = valid ? checked(((attempts - 1) * batchBoundTokens) + usageTokens!.Value) : reserved;
        return Price(Math.Min(tokens, reserved), options);
    }

    static IndexExposure Price(long tokens, PrompterOptions options) => new(tokens, tokens * options.Ingestion.PricePerMillionTokens / 1_000_000m);
}
