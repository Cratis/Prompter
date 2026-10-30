// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.AI;

namespace Cratis.Prompter.Embeddings;

/// <summary>
/// Bounded per-task retry settings shared by every attempt of one embedding call.
/// </summary>
public sealed record EmbeddingRetryPolicy
{
    /// <summary>
    /// Gets the fixed task label.
    /// </summary>
    public required string Task { get; init; }

    /// <summary>
    /// Gets the retry ceiling.
    /// </summary>
    public required int MaxRetries { get; init; }

    /// <summary>
    /// Gets the complete call deadline.
    /// </summary>
    public required TimeSpan Budget { get; init; }

    /// <summary>
    /// Gets the fallback backoff base.
    /// </summary>
    public required TimeSpan BaseDelay { get; init; }

    /// <summary>
    /// Selects indexing or interactive limits, clamping even misconfigured bounds.
    /// </summary>
    /// <param name="configuration">Provider settings.</param>
    /// <param name="request">The task marker.</param>
    /// <returns>One bounded retry policy.</returns>
    public static EmbeddingRetryPolicy For(VoyageOptions configuration, EmbeddingGenerationOptions? request)
    {
        var indexing = request?.AdditionalProperties?.TryGetValue(EmbeddingRetry.TaskKey, out var task) == true && string.Equals(task as string, "index", StringComparison.Ordinal);
        var retries = indexing ? Math.Clamp(configuration.IndexMaxRetries, 0, 5) : Math.Clamp(configuration.MaxRetries, 0, 3);
        var seconds = indexing ? Math.Clamp(configuration.IndexTimeoutSeconds, 1, 120) : Math.Clamp(configuration.TimeoutSeconds, 1, 60);
        return new()
        {
            Task = indexing ? "index" : "query",
            MaxRetries = retries,
            Budget = TimeSpan.FromSeconds(seconds),
            BaseDelay = TimeSpan.FromMilliseconds(Math.Clamp(configuration.RetryBaseDelayMilliseconds, 0, 30_000))
        };
    }

    /// <summary>
    /// Honors a provider minimum delay or applies full jitter to exponential fallback backoff.
    /// </summary>
    /// <param name="failure">The sanitized provider failure.</param>
    /// <param name="attempt">Zero-based retry number.</param>
    /// <returns>The minimum wait.</returns>
    public TimeSpan Delay(HttpRequestException failure, int attempt) => failure is VoyageRequestFailed { RetryAfter: { } hint }
        ? hint : EmbeddingRetry.BackoffFor(attempt, BaseDelay) * Random.Shared.NextDouble();
}
