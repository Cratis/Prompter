// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Prompter.Embeddings;

/// <summary>
/// Shared key-scoped cooldown. Concurrent observations may extend but never shorten it.
/// </summary>
public sealed class EmbeddingCooldown
{
    long _until;

    /// <summary>
    /// Waits without ignoring a provider hint or exceeding the task deadline.
    /// </summary>
    /// <param name="delay">The minimum delay.</param>
    /// <param name="deadline">The whole-call deadline.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The delay task.</returns>
    /// <exception cref="EmbeddingGenerationFailed">The provider delay exceeds the remaining deadline.</exception>
    public static async Task WaitFor(TimeSpan delay, DateTimeOffset deadline, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (delay >= deadline - DateTimeOffset.UtcNow)
        {
            throw new EmbeddingGenerationFailed("provider cooldown exceeds the remaining request deadline");
        }

        await Task.Delay(delay > TimeSpan.Zero ? delay : TimeSpan.Zero, token);
    }

    /// <summary>
    /// Waits for the latest observed key cooldown.
    /// </summary>
    /// <param name="deadline">The whole-call deadline.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>The wait task.</returns>
    public async Task Wait(DateTimeOffset deadline, CancellationToken token)
    {
        TimeSpan remaining;
        while ((remaining = new DateTimeOffset(Interlocked.Read(ref _until), TimeSpan.Zero) - DateTimeOffset.UtcNow) > TimeSpan.Zero)
        {
            await WaitFor(remaining, deadline, token);
        }
    }

    /// <summary>
    /// Records a rate-limit cooldown, without recording provider text or account identifiers.
    /// </summary>
    /// <param name="status">The HTTP status.</param>
    /// <param name="delay">The provider delay or jittered fallback.</param>
    public void Observe(HttpStatusCode? status, TimeSpan delay)
    {
        if (status != HttpStatusCode.TooManyRequests)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var until = now.UtcTicks + Math.Min(delay.Ticks, DateTimeOffset.MaxValue.UtcTicks - now.UtcTicks);
        long observed;
        do
        {
            observed = Interlocked.Read(ref _until);
        }
        while (observed < until && Interlocked.CompareExchange(ref _until, until, observed) != observed);
    }
}
