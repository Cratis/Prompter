// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Embeddings;

/// <summary>
/// Retries bounded transient embedding failures with provider hints, shared cooldown and accounting.
/// </summary>
/// <param name="inner">The embedding generator being wrapped.</param>
/// <param name="prompterOptions">The provider retry configuration.</param>
/// <param name="logger">Logger for sanitized diagnostics.</param>
/// <param name="accounting">Optional shared reservation/accounting integration.</param>
public sealed class ResilientEmbeddingGenerator(
    IEmbeddingGenerator<string, Embedding<float>> inner,
    IOptions<PrompterOptions> prompterOptions,
    ILogger<ResilientEmbeddingGenerator> logger,
    IProviderAccounting? accounting = null)
    : IEmbeddingGenerator<string, Embedding<float>>
{
    readonly EmbeddingCooldown _cooldown = new();

    /// <inheritdoc/>
    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var inputs = values as IReadOnlyList<string> ?? [.. values];
        var voyage = prompterOptions.Value.Voyage;
        var policy = EmbeddingRetryPolicy.For(voyage, options);
        var deadline = DateTimeOffset.UtcNow + policy.Budget;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(policy.Budget);
        var operation = Guid.NewGuid();
        var inputBound = inputs.Sum(Encoding.UTF8.GetByteCount);
        for (var attempt = 0; ; attempt++)
        {
            timeout.Token.ThrowIfCancellationRequested();
            await _cooldown.Wait(deadline, timeout.Token);
            var dispatch = new ProviderDispatch(operation, "voyage", "primary", policy.Task, attempt + 1, inputBound, 0, voyage.Model, string.Empty);
            try
            {
                return await GenerateAttempt(inputs, options, dispatch, timeout.Token);
            }
            catch (HttpRequestException failure) when (EmbeddingRetry.IsTransient(failure.StatusCode))
            {
                var delay = policy.Delay(failure, attempt);
                _cooldown.Observe(failure.StatusCode, delay);
                if (attempt >= policy.MaxRetries)
                {
                    throw;
                }

                logger.RetryingAfterTransientFailure((int?)failure.StatusCode, attempt + 1, policy.MaxRetries, delay.TotalMilliseconds);
                await EmbeddingCooldown.WaitFor(delay, deadline, timeout.Token);
            }
        }
    }

    /// <inheritdoc/>
    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is null && serviceType.IsInstanceOfType(this) ? this : inner.GetService(serviceType, serviceKey);

    /// <inheritdoc/>
    public void Dispose() => inner.Dispose();

    async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAttempt(IReadOnlyList<string> inputs, EmbeddingGenerationOptions? options, ProviderDispatch dispatch, CancellationToken token)
    {
        if (accounting is not null && !await accounting.Reserve(dispatch, token))
        {
            throw new EmbeddingGenerationFailed("budget unavailable");
        }

        GeneratedEmbeddings<Embedding<float>>? response = null;
        try
        {
            response = await inner.GenerateAsync(inputs, options, token);
            return response;
        }
        finally
        {
            if (accounting is not null)
            {
                await accounting.Complete(dispatch, response?.Usage, response is null ? "unknown" : "stop");
            }
        }
    }
}
