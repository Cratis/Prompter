// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Embeddings;

/// <summary>
/// Wraps an <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> with retry and exponential backoff on
/// transient failures (Voyage rate limits and server errors), so a whole index run does not fail on a single
/// throttled batch. The policy itself lives in <see cref="EmbeddingRetry"/> and is specified in isolation.
/// </summary>
/// <param name="inner">The embedding generator being wrapped.</param>
/// <param name="prompterOptions">The Prompter options carrying the Voyage retry configuration.</param>
/// <param name="logger">Logger for diagnostics.</param>
public sealed class ResilientEmbeddingGenerator(
    IEmbeddingGenerator<string, Embedding<float>> inner,
    IOptions<PrompterOptions> prompterOptions,
    ILogger<ResilientEmbeddingGenerator> logger)
    : IEmbeddingGenerator<string, Embedding<float>>
{
    /// <inheritdoc/>
    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Materialize once so the input is safe to hand to the inner generator again on a retry.
        var inputs = values as IReadOnlyList<string> ?? [.. values];
        var voyage = prompterOptions.Value.Voyage;
        var baseDelay = TimeSpan.FromMilliseconds(voyage.RetryBaseDelayMilliseconds);

        for (var attempt = 0; ; attempt++)
        {
            try
            {
                return await inner.GenerateAsync(inputs, options, cancellationToken);
            }
            catch (HttpRequestException exception) when (attempt < voyage.MaxRetries && EmbeddingRetry.IsTransient(exception.StatusCode))
            {
                var delay = EmbeddingRetry.BackoffFor(attempt, baseDelay);
                logger.RetryingAfterTransientFailure((int?)exception.StatusCode, attempt + 1, voyage.MaxRetries, delay.TotalMilliseconds);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    /// <inheritdoc/>
    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : inner.GetService(serviceType, serviceKey);

    /// <inheritdoc/>
    public void Dispose() => inner.Dispose();
}
