// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Protection;

/// <summary>Bounded in-memory pauses that grow when a requester keeps being refused. The owning admission lock serializes access.</summary>
/// <param name="capacity">Maximum tracked requesters.</param>
internal sealed class EscalatingCooldowns(int capacity)
{
    static readonly TimeSpan _levelMemory = TimeSpan.FromHours(24);
    readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    /// <summary>Gets the remaining time of an active pause.</summary>
    /// <param name="key">The anonymous requester key.</param>
    /// <param name="now">The expiry clock.</param>
    /// <returns>The remaining pause, or <see langword="null"/> when there is none.</returns>
    public TimeSpan? Remaining(string key, DateTimeOffset now)
    {
        Expire(now);
        return _entries.TryGetValue(key, out var entry) && entry.BlockedUntil > now ? entry.BlockedUntil - now : null;
    }

    /// <summary>Records a requester-attributable refusal.</summary>
    /// <param name="key">The anonymous requester key.</param>
    /// <param name="now">The expiry clock.</param>
    /// <param name="options">The protection configuration.</param>
    /// <returns><see langword="true"/> when this refusal started a pause.</returns>
    public bool Strike(string key, DateTimeOffset now, ProtectionOptions options)
    {
        Expire(now);
        if (options.RefusalStrikes <= 0)
        {
            return false;
        }

        if (!_entries.TryGetValue(key, out var entry))
        {
            if (_entries.Count >= capacity)
            {
                return false;
            }

            entry = new Entry { ForgetAt = now.AddSeconds(options.RefusalStrikeWindowSeconds) };
            _entries[key] = entry;
        }

        if (entry.BlockedUntil > now)
        {
            return false;
        }

        if (entry.Strikes == 0 || entry.StrikeWindowEnds <= now)
        {
            entry.Strikes = 0;
            entry.StrikeWindowEnds = now.AddSeconds(options.RefusalStrikeWindowSeconds);
        }

        entry.Strikes++;
        if (entry.Strikes < options.RefusalStrikes)
        {
            entry.ForgetAt = Max(entry.ForgetAt, entry.StrikeWindowEnds);
            return false;
        }

        entry.Level++;
        var seconds = Math.Min(options.EscalatedCooldownSeconds * Math.Pow(2, Math.Min(entry.Level - 1, 30)), options.MaxEscalatedCooldownSeconds);
        entry.BlockedUntil = now.AddSeconds(seconds);
        entry.Strikes = 0;
        entry.ForgetAt = entry.BlockedUntil + _levelMemory;
        return true;
    }

    static DateTimeOffset Max(DateTimeOffset first, DateTimeOffset second) => first >= second ? first : second;

    void Expire(DateTimeOffset now)
    {
        foreach (var key in _entries.Where(pair => pair.Value.ForgetAt <= now).Select(pair => pair.Key).ToArray())
        {
            _entries.Remove(key);
        }
    }

    sealed class Entry
    {
        public int Strikes { get; set; }

        public DateTimeOffset StrikeWindowEnds { get; set; }

        public int Level { get; set; }

        public DateTimeOffset BlockedUntil { get; set; }

        public DateTimeOffset ForgetAt { get; set; }
    }
}
