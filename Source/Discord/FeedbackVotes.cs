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
/// Bounds feedback button clicks: a per-user click budget and the latest persisted verdict per answer, and serializes each answer's click lifecycle. All are
/// short-lived, bounded local memory that fails closed when full, and users are kept only as keyed hashes.
/// </summary>
/// <param name="options">The protection configuration.</param>
/// <param name="clock">The expiry clock.</param>
public sealed class FeedbackVotes(IOptions<PrompterOptions> options, TimeProvider clock)
{
    static readonly TimeSpan _turnTimeout = TimeSpan.FromSeconds(2);

    readonly Lock _lock = new();
    readonly byte[] _key = RandomNumberGenerator.GetBytes(32);
    readonly TransientLimits _budgets = new(options.Value.Protection.LocalCapacity);
    readonly Dictionary<long, Latest> _latest = [];
    readonly Dictionary<long, Gate> _gates = [];
    readonly Dictionary<string, Gate> _users = [];

    /// <summary>Spends one click from the user's budget.</summary>
    /// <param name="userId">The clicking Discord user.</param>
    /// <returns>Whether the click is within budget; false also when local memory is full.</returns>
    public bool TrySpend(ulong userId)
    {
        var limits = options.Value.Protection;
        var key = Hash(userId);
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

    /// <summary>
    /// Waits for this answer's turn, so concurrent clicks on one answer check, acknowledge, write and commit one at
    /// a time. Waiting is bounded, and so is the number of answers with a click in flight; both fail closed.
    /// </summary>
    /// <param name="interactionId">The answer being voted on.</param>
    /// <param name="userId">The clicking user, retained only as a keyed hash.</param>
    /// <returns>The turn to dispose when the click is finished, or <see langword="null"/> when it must be refused.</returns>
    public async Task<FeedbackTurn?> Enter(long interactionId, ulong userId)
    {
        using var wait = new CancellationTokenSource(_turnTimeout, clock);
        var user = await EnterGate(_users, Hash(userId), wait.Token);
        if (user is null)
        {
            return null;
        }

        var answer = await EnterGate(_gates, interactionId, wait.Token);
        if (answer is null)
        {
            user.Dispose();
            return null;
        }

        return new(() =>
        {
            answer.Dispose();
            user.Dispose();
        });
    }

    /// <summary>Compares a click with the latest persisted verdict for its answer. Call it inside the answer's turn.</summary>
    /// <param name="click">The decoded click.</param>
    /// <returns>What the handler should do with the click.</returns>
    public FeedbackVote Check(FeedbackClick click)
    {
        var limits = options.Value.Protection;
        lock (_lock)
        {
            Sweep(clock.GetUtcNow());
            if (_latest.TryGetValue(click.InteractionId, out var known))
            {
                return known.Verdict == click.Verdict ? FeedbackVote.Repeat : FeedbackVote.Record;
            }

            return _latest.Count >= limits.LocalCapacity ? FeedbackVote.Refused : FeedbackVote.Record;
        }
    }

    /// <summary>Remembers a verdict once it has been persisted. A failed write is simply never committed.</summary>
    /// <param name="click">The decoded click whose write succeeded.</param>
    public void Commit(FeedbackClick click)
    {
        var limits = options.Value.Protection;
        lock (_lock)
        {
            var now = clock.GetUtcNow();
            if (_latest.ContainsKey(click.InteractionId) || _latest.Count < limits.LocalCapacity)
            {
                _latest[click.InteractionId] = new(click.Verdict, now + TimeSpan.FromSeconds(limits.FeedbackWindowSeconds));
            }
        }
    }

    async Task<FeedbackTurn?> EnterGate<T>(Dictionary<T, Gate> gates, T key, CancellationToken cancellationToken)
        where T : notnull
    {
        var gate = Acquire(gates, key);
        if (gate is null)
        {
            return null;
        }

        try
        {
            await gate.Semaphore.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Leave(gates, key, gate, false);
            return null;
        }

        return new(() => Leave(gates, key, gate, true));
    }

    string Hash(ulong userId) => Convert.ToHexString(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(userId.ToString(CultureInfo.InvariantCulture))));

    Gate? Acquire<T>(Dictionary<T, Gate> gates, T key)
        where T : notnull
    {
        lock (_lock)
        {
            if (!gates.TryGetValue(key, out var gate))
            {
                if (gates.Count >= options.Value.Protection.LocalCapacity)
                {
                    return null;
                }

                gate = new();
                gates[key] = gate;
            }

            gate.Users++;
            return gate;
        }
    }

    void Leave<T>(Dictionary<T, Gate> gates, T key, Gate gate, bool held)
        where T : notnull
    {
        lock (_lock)
        {
            gate.Users--;
            if (gate.Users == 0)
            {
                gates.Remove(key);
            }
        }

        if (held)
        {
            gate.Semaphore.Release();
        }
    }

    void Sweep(DateTimeOffset now)
    {
        foreach (var expired in _latest.Where(pair => pair.Value.Expires <= now).Select(pair => pair.Key).ToArray())
        {
            _latest.Remove(expired);
        }
    }

    sealed class Gate
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);

        public int Users { get; set; }
    }

    sealed record Latest(FeedbackVerdict Verdict, DateTimeOffset Expires);
}
