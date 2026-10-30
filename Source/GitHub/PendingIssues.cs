// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Prompter.GitHub;

/// <summary>
/// Bounded, transient, single-use consent for exactly one delivered preview.
/// </summary>
/// <param name="timeProvider">The expiry clock.</param>
public class PendingIssues(TimeProvider timeProvider)
{
    /// <summary>
    /// The maximum number of provisional and active previews retained.
    /// </summary>
    public const int Capacity = 200;

    /// <summary>
    /// The maximum time a draft may be held, including preview delivery.
    /// </summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);

    readonly Lock _lock = new();
    readonly Dictionary<string, Entry> _entries = [];

    /// <summary>
    /// Holds a provisional payload, delivers it, then binds it to the returned message.
    /// </summary>
    /// <param name="payload">The exact, already rendered payload.</param>
    /// <param name="consent">The admitted reporter and public scope.</param>
    /// <param name="send">Delivers the full preview and buttons; returns its Discord message ID.</param>
    /// <returns>The unpredictable consent token. Delivery failure revokes it.</returns>
    public async Task<string> Present(PendingIssue payload, IssueConsent consent, Func<string, Task<ulong>> send)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        lock (_lock)
        {
            Prune();
            if (_entries.Count >= Capacity)
            {
                _entries.Remove(_entries.First().Key);
            }
            _entries.Add(token, new(payload, consent, timeProvider.GetUtcNow(), 0));
        }
        try
        {
            var message = await send(token);
            lock (_lock)
            {
                Prune();
                if (message == 0 || !_entries.TryGetValue(token, out var entry))
                {
                    throw new PreviewUnavailable();
                }
                _entries[token] = entry with { Message = message };
            }
            return token;
        }
        catch
        {
            lock (_lock)
            {
                _entries.Remove(token);
            }
            throw;
        }
    }

    /// <summary>
    /// Atomically consumes an active matching preview for either filing or cancellation.
    /// </summary>
    /// <param name="token">The random token from the button.</param>
    /// <param name="consent">The identity and scope of the click.</param>
    /// <param name="message">The clicked preview's message ID.</param>
    /// <returns>The payload, or null without consuming another reporter's preview.</returns>
    public PendingIssue? Take(string token, IssueConsent consent, ulong message)
    {
        lock (_lock)
        {
            Prune();
            if (!_entries.TryGetValue(token, out var entry) || !Matches(entry, consent, message))
            {
                return null;
            }
            _entries.Remove(token);
            return entry.Payload;
        }
    }

    static bool Matches(Entry entry, IssueConsent consent, ulong message) => entry.Message != 0 && entry.Message == message && entry.Consent == consent;

    void Prune()
    {
        var now = timeProvider.GetUtcNow();
        foreach (var token in _entries.Where(pair => now - pair.Value.Created >= Lifetime).Select(pair => pair.Key).ToArray())
        {
            _entries.Remove(token);
        }
    }

    sealed record Entry(PendingIssue Payload, IssueConsent Consent, DateTimeOffset Created, ulong Message);
}
