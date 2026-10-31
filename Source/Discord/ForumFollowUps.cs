// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Discord;

/// <summary>
/// An in-memory, bounded, expiring record of the help-forum threads Prompter answered and whether a human helper
/// has since joined. Answer threads also retain their asker's identifier in memory; no content is kept.
/// </summary>
/// <param name="options">The Prompter options carrying the local capacity bound.</param>
/// <param name="clock">The clock measuring expiry.</param>
public sealed class ForumFollowUps(IOptions<PrompterOptions> options, TimeProvider clock)
{
    /// <summary>How long a thread is followed after Prompter's last answer in it.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    readonly Lock _lock = new();
    readonly Dictionary<ulong, Entry> _threads = [];

    /// <summary>Records that Prompter answered in a thread, refreshing its expiry and keeping any hand-off.</summary>
    /// <param name="threadId">The thread identifier.</param>
    public void Answered(ulong threadId) => Track(threadId, null);

    /// <summary>Tracks a newly opened answer thread and its asker.</summary>
    /// <param name="threadId">The thread identifier.</param>
    /// <param name="askerId">The asker identifier.</param>
    public void Opened(ulong threadId, ulong askerId) => Track(threadId, askerId);

    /// <summary>Finds the asker of an unexpired answer thread, even after hand-off.</summary>
    /// <param name="threadId">The thread identifier.</param>
    /// <returns>The asker, or null for untracked or forum threads.</returns>
    public ulong? AskerOf(ulong threadId)
    {
        lock (_lock)
        {
            return _threads.TryGetValue(threadId, out var entry) && entry.Expires > clock.GetUtcNow() ? entry.Asker : null;
        }
    }

    /// <summary>Records that another human joined a tracked thread. Never starts tracking a thread.</summary>
    /// <param name="threadId">The thread identifier.</param>
    public void HandOff(ulong threadId)
    {
        lock (_lock)
        {
            if (_threads.TryGetValue(threadId, out var existing))
            {
                _threads[threadId] = existing with { HandedOff = true };
            }
        }
    }

    /// <summary>Checks whether the thread author's messages are still answered without a mention.</summary>
    /// <param name="threadId">The thread identifier.</param>
    /// <returns>Whether the thread is tracked, unexpired and not handed off.</returns>
    public bool Accepts(ulong threadId)
    {
        lock (_lock)
        {
            return _threads.TryGetValue(threadId, out var entry) && !entry.HandedOff && entry.Expires > clock.GetUtcNow();
        }
    }

    void Track(ulong threadId, ulong? asker)
    {
        lock (_lock)
        {
            var now = clock.GetUtcNow();
            var expires = now.Add(Lifetime);
            if (_threads.TryGetValue(threadId, out var existing))
            {
                _threads[threadId] = existing with { Expires = expires, Asker = asker ?? existing.Asker };
                return;
            }

            foreach (var stale in _threads.Where(pair => pair.Value.Expires <= now).Select(pair => pair.Key).ToList())
            {
                _threads.Remove(stale);
            }

            if (_threads.Count >= options.Value.Protection.LocalCapacity)
            {
                _threads.Remove(_threads.MinBy(pair => pair.Value.Expires).Key);
            }

            _threads[threadId] = new(false, expires, asker);
        }
    }

    sealed record Entry(bool HandedOff, DateTimeOffset Expires, ulong? Asker);
}
