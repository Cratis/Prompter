// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Discord;

/// <summary>
/// Bounds feedback button clicks: a per-user click budget and the latest verdict seen per answer. Both are
/// short-lived, bounded local memory that fails closed when full, and users are kept only as keyed hashes.
/// </summary>
/// <param name="options">The protection configuration.</param>
/// <param name="clock">The expiry clock.</param>
public sealed class FeedbackVotes(IOptions<PrompterOptions> options, TimeProvider clock)
{
    readonly Lock _lock = new();
    readonly byte[] _key = RandomNumberGenerator.GetBytes(32);
    readonly TransientLimits _budgets = new(options.Value.Protection.LocalCapacity);
    readonly Dictionary<long, Latest> _latest = [];

    /// <summary>Spends one click from the user's budget.</summary>
    /// <param name="userId">The clicking Discord user.</param>
    /// <returns>Whether the click is within budget; false also when local memory is full.</returns>
    public bool TrySpend(ulong userId)
    {
        var limits = options.Value.Protection;
        var key = Convert.ToHexString(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(userId.ToString(CultureInfo.InvariantCulture))));
        lock (_lock)
        {
            var now = clock.GetUtcNow();
            if (!_budgets.Available(key, limits.FeedbackClicksPerUser, now))
            {
                return false;
            }

            _budgets.Consume(key, TimeSpan.FromSeconds(limits.FeedbackWindowSeconds), now);
            return true;
        }
    }

    /// <summary>Compares a click with the latest verdict for its answer and remembers it.</summary>
    /// <param name="click">The decoded click.</param>
    /// <returns>What the handler should do with the click.</returns>
    public FeedbackVote Remember(FeedbackClick click)
    {
        var limits = options.Value.Protection;
        lock (_lock)
        {
            var now = clock.GetUtcNow();
            foreach (var expired in _latest.Where(pair => pair.Value.Expires <= now).Select(pair => pair.Key).ToArray())
            {
                _latest.Remove(expired);
            }

            if (_latest.TryGetValue(click.InteractionId, out var known) && known.Verdict == click.Verdict)
            {
                return FeedbackVote.Repeat;
            }

            if (!_latest.ContainsKey(click.InteractionId) && _latest.Count >= limits.LocalCapacity)
            {
                return FeedbackVote.Refused;
            }

            _latest[click.InteractionId] = new(click.Verdict, now + TimeSpan.FromSeconds(limits.FeedbackWindowSeconds));
            return FeedbackVote.Record;
        }
    }

    /// <summary>Forgets a remembered verdict after its write failed, so a retry is recorded.</summary>
    /// <param name="click">The decoded click whose write failed.</param>
    public void Forget(FeedbackClick click)
    {
        lock (_lock)
        {
            _latest.Remove(click.InteractionId);
        }
    }

    sealed record Latest(FeedbackVerdict Verdict, DateTimeOffset Expires);
}
