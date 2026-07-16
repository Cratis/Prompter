// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Discord;

/// <summary>
/// A per-user token bucket limiting how many questions a user may ask within a rolling window.
/// The bot runs single-instance, so the buckets live in memory. Refills are derived from the time
/// passed in by the caller, keeping the logic pure and deterministic for testing.
/// </summary>
/// <param name="options">The Prompter options carrying the Discord rate-limit configuration.</param>
public class RateLimiter(IOptions<PrompterOptions> options)
{
    readonly ConcurrentDictionary<string, Bucket> _buckets = new();
    readonly RateLimitOptions _limit = options.Value.Discord.RateLimit;

    /// <summary>
    /// Attempts to spend one question's allowance for a user, refilling the bucket for the elapsed time first.
    /// </summary>
    /// <param name="userKey">
    /// An in-memory identifier for the asking user (their id), used only to bucket requests. It is never
    /// stored or logged, so it needs no hashing - the interaction log keeps no user identity at all (D-13).
    /// </param>
    /// <param name="now">The current time, used to refill the bucket.</param>
    /// <returns>
    /// <see langword="true"/> when the question is allowed; <see langword="false"/> when the user is over
    /// their limit.
    /// </returns>
    public bool TryConsume(string userKey, DateTimeOffset now)
    {
        double capacity = _limit.MaxQuestions;
        var refillPerSecond = capacity / TimeSpan.FromMinutes(_limit.WindowMinutes).TotalSeconds;
        var bucket = _buckets.GetOrAdd(
            userKey,
            static (_, seed) => new Bucket(seed.Capacity, seed.Now),
            (Capacity: capacity, Now: now));

        lock (bucket)
        {
            var elapsedSeconds = (now - bucket.LastRefill).TotalSeconds;
            if (elapsedSeconds > 0)
            {
                bucket.Tokens = Math.Min(capacity, bucket.Tokens + (elapsedSeconds * refillPerSecond));
                bucket.LastRefill = now;
            }

            if (bucket.Tokens < 1)
            {
                return false;
            }

            bucket.Tokens--;
            return true;
        }
    }

    sealed class Bucket(double tokens, DateTimeOffset lastRefill)
    {
        public double Tokens { get; set; } = tokens;

        public DateTimeOffset LastRefill { get; set; } = lastRefill;
    }
}
