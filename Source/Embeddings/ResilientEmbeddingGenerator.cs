// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;
using Cratis.Prompter.Protection;
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
        var inputBound = checked((int)inputs.Sum(input => policy.Task == "index"
            ? EmbeddingInputLimits.TokensFor(input) : Encoding.UTF8.GetByteCount(input)));
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

                ProviderMetrics.Retry("voyage", delay);
                if (failure.StatusCode == System.Net.HttpStatusCode.TooManyRequests) ProviderMetrics.Event("voyage", "cooldown");
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
        using var attempt = accounting is null ? null : ProviderAttemptScope.Enter(dispatch, inputs.Count);
        if (accounting is not null && !await accounting.Reserve(dispatch, token))
        {
            ProviderMetrics.Event("voyage", "denied");
            throw new EmbeddingGenerationFailed("budget unavailable");
        }

        GeneratedEmbeddings<Embedding<float>>? response = null;
        var outcome = "unknown";
        var start = Stopwatch.GetTimestamp();
        ProviderMetrics.Begin(dispatch);
        try
        {
            response = await inner.GenerateAsync(inputs, options, token);
            outcome = "success";
            return response;
        }
        catch (Exception error)
        {
            outcome = ProviderMetrics.Category(error);
            throw;
        }
        finally
        {
            ProviderMetrics.Complete(dispatch, new(response?.Usage, outcome, Stopwatch.GetElapsedTime(start)), prompterOptions.Value.Protection.Voyage);
            if (accounting is not null)
            {
                await accounting.Complete(dispatch, response?.Usage, response is null ? "unknown" : "stop");
            }
        }
    }
}
