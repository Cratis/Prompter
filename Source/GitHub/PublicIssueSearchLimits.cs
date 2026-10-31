// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.GitHub;

/// <summary>
/// Process-wide request limiter for anonymous public searches: no waiting queue, deliberately below GitHub's
/// unauthenticated search limit, plus a bounded cooldown after GitHub signals a limit.
/// </summary>
/// <param name="clock">The clock.</param>
public sealed class PublicIssueSearchLimits(TimeProvider clock)
{
    /// <summary>The most requests allowed per minute.</summary>
    public const int PerMinute = 8;

    /// <summary>The most requests allowed per hour.</summary>
    public const int PerHour = 40;

    /// <summary>The cooldown applied when a limit is signalled without a usable reset hint.</summary>
    public static readonly TimeSpan DefaultCooldown = TimeSpan.FromMinutes(1);

    /// <summary>The longest cooldown any hint can impose.</summary>
    public static readonly TimeSpan MaximumCooldown = TimeSpan.FromMinutes(15);

    readonly Lock _gate = new();
    readonly Queue<DateTimeOffset> _requests = new();
    DateTimeOffset _cooldownUntil = DateTimeOffset.MinValue;

    /// <summary>Tries to take one request slot without waiting.</summary>
    /// <returns>Whether a request may be sent now.</returns>
    public bool TryAcquire()
    {
        lock (_gate)
        {
            var now = clock.GetUtcNow();
            if (now < _cooldownUntil)
            {
                return false;
            }

            while (_requests.Count > 0 && now - _requests.Peek() >= TimeSpan.FromHours(1))
            {
                _requests.Dequeue();
            }

            if (_requests.Count >= PerHour || _requests.Count(at => now - at < TimeSpan.FromMinutes(1)) >= PerMinute)
            {
                return false;
            }

            _requests.Enqueue(now);
            return true;
        }
    }

    /// <summary>Starts a bounded cooldown after a rate-limit response.</summary>
    /// <param name="requested">The server-provided wait, or <see langword="null"/> when absent or unusable.</param>
    public void CoolDown(TimeSpan? requested)
    {
        var wait = requested is { } value && value > TimeSpan.Zero ? TimeSpan.FromTicks(Math.Min(value.Ticks, MaximumCooldown.Ticks)) : DefaultCooldown;
        lock (_gate)
        {
            var until = clock.GetUtcNow() + wait;
            if (until > _cooldownUntil)
            {
                _cooldownUntil = until;
            }
        }
    }
}
