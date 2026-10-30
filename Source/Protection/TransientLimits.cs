// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Protection;

/// <summary>Bounded expiring local counters. The owning admission lock serializes access.</summary>
/// <param name="capacity">Maximum retained counters.</param>
internal sealed class TransientLimits(int capacity)
{
    readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    /// <summary>Checks availability without consuming it.</summary>
    /// <param name="key">The anonymous counter key.</param>
    /// <param name="maximum">The allowed count.</param>
    /// <param name="now">The expiry clock.</param>
    /// <returns>Whether the counter has capacity.</returns>
    public bool Available(string key, int maximum, DateTimeOffset now)
    {
        Expire(now);
        return _entries.TryGetValue(key, out var entry) ? entry.Count < maximum : maximum > 0 && _entries.Count < capacity;
    }

    /// <summary>Consumes previously checked capacity.</summary>
    /// <param name="key">The anonymous counter key.</param>
    /// <param name="window">The counter lifetime.</param>
    /// <param name="now">The expiry clock.</param>
    public void Consume(string key, TimeSpan window, DateTimeOffset now)
    {
        if (_entries.TryGetValue(key, out var entry))
        {
            _entries[key] = entry with { Count = entry.Count + 1 };
        }
        else
        {
            _entries.Add(key, new(1, now + window));
        }
    }

    void Expire(DateTimeOffset now)
    {
        foreach (var key in _entries.Where(pair => pair.Value.Expires <= now).Select(pair => pair.Key).ToArray())
        {
            _entries.Remove(key);
        }
    }

    sealed record Entry(int Count, DateTimeOffset Expires);
}
