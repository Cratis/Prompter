// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Globalization;

namespace Cratis.Prompter.GitHub;

/// <summary>
/// Holds drafted issues between showing the preview and the reporter confirming it.
/// </summary>
/// <remarks>
/// In memory only, and deliberately so. A Discord custom id is capped at 100 characters, far too small to
/// carry an issue body, so the preview's buttons carry a short token that resolves back to the draft here.
/// Nothing is written to the database: a draft that is never confirmed leaves no trace, which is what keeps
/// filing consent-in-the-moment under D-13. A restart loses pending drafts — the reporter is told the draft
/// expired and can run the command again, which is a far better failure than persisting conversation text.
/// </remarks>
/// <param name="timeProvider">The clock expiry is measured against.</param>
public class PendingIssues(TimeProvider timeProvider)
{
    /// <summary>
    /// The most drafts held at once, after which the oldest are dropped. A bound rather than a limit anyone
    /// should reach: it stops a burst of unconfirmed drafts growing the process indefinitely.
    /// </summary>
    public const int Capacity = 200;

    /// <summary>
    /// How long a draft stays available for confirmation.
    /// </summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);

    readonly ConcurrentDictionary<string, Pending> _drafts = new(StringComparer.Ordinal);
    long _next;

    /// <summary>
    /// Holds a draft and returns the token identifying it.
    /// </summary>
    /// <param name="draft">The drafted issue.</param>
    /// <param name="repository">The repository the preview offered to file it in.</param>
    /// <param name="conversationUrl">A link back to the conversation, if available.</param>
    /// <returns>The token to put on the preview's buttons.</returns>
    public string Hold(IssueDraft draft, string repository, string? conversationUrl)
    {
        Evict();

        var sequence = Interlocked.Increment(ref _next);
        var token = sequence.ToString(CultureInfo.InvariantCulture);
        _drafts[token] = new Pending(draft, repository, conversationUrl, timeProvider.GetUtcNow(), sequence);

        return token;
    }

    /// <summary>
    /// Takes a held draft, removing it so a double-click cannot file the same issue twice.
    /// </summary>
    /// <param name="token">The token from the clicked button.</param>
    /// <returns>The held draft, or <see langword="null"/> when it is unknown or has expired.</returns>
    public PendingIssue? Take(string token)
    {
        if (!_drafts.TryRemove(token, out var pending))
        {
            return null;
        }

        return timeProvider.GetUtcNow() - pending.HeldAt > Lifetime
            ? null
            : new PendingIssue(pending.Draft, pending.Repository, pending.ConversationUrl);
    }

    void Evict()
    {
        var now = timeProvider.GetUtcNow();

        foreach (var (token, pending) in _drafts)
        {
            if (now - pending.HeldAt > Lifetime)
            {
                _drafts.TryRemove(token, out _);
            }
        }

        if (_drafts.Count < Capacity)
        {
            return;
        }

        // Ordered by insertion, not by time: several drafts can be held within one clock tick, and
        // "oldest" has to mean the one held first rather than whichever the timestamps happen to tie on.
        foreach (var (token, _) in _drafts.OrderBy(entry => entry.Value.Sequence).Take(_drafts.Count - Capacity + 1))
        {
            _drafts.TryRemove(token, out _);
        }
    }

    sealed record Pending(
        IssueDraft Draft,
        string Repository,
        string? ConversationUrl,
        DateTimeOffset HeldAt,
        long Sequence);
}
