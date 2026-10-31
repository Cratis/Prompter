// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Cratis.Prompter.Protection;

/// <summary>Nonqueued global concurrency and durable whole-answer exposure before history or provider work.</summary>
/// <param name="ledger">The durable ledger.</param>
/// <param name="options">The verified exposure limits.</param>
/// <param name="clock">The request deadline clock.</param>
public sealed class PaidWork(ISpendLedger ledger, IOptions<PrompterOptions> options, TimeProvider clock) : IDisposable
{
    readonly SemaphoreSlim _slots = new(options.Value.Protection.Concurrency);
    readonly Lock _lock = new();
    readonly TransientLimits _hourly = new(1);

    /// <summary>Reserves an entire interactive operation, including Voyage retry exposure.</summary>
    /// <param name="cancellationToken">The caller cancellation.</param>
    /// <returns>An expiring lease; refusal never queues or dispatches.</returns>
    /// <exception cref="WorkRefused">Concurrency, pricing, daily capacity, or the ledger prevents admission.</exception>
    public async Task<WorkLease> BeginInteractive(CancellationToken cancellationToken = default)
    {
        CheckEnabled();
        if (!_slots.Wait(0, cancellationToken))
        {
            throw Refused("concurrency");
        }

        try
        {
            ConsumeHourly();
            return await ReserveLease(cancellationToken);
        }
        catch
        {
            _slots.Release();
            throw;
        }
    }

    /// <inheritdoc/>
    public void Dispose() => _slots.Dispose();

    static WorkRefused Refused(string reason)
    {
        ProviderMetrics.Event("protection", reason);
        return new();
    }

    void CheckEnabled()
    {
        var config = options.Value;
        if (!config.Protection.AnsweringEnabled)
        {
            throw Refused("disabled");
        }

        if (!config.Protection.Anthropic.Covers(config.Anthropic.Model, true) || !config.Protection.Voyage.Covers(config.Voyage.Model, false))
        {
            throw Refused("pricing");
        }
    }

    void ConsumeHourly()
    {
        lock (_lock)
        {
            var now = clock.GetUtcNow();
            if (!_hourly.Available("global", options.Value.Protection.GlobalPerHour, now))
            {
                throw Refused("hourly");
            }

            _hourly.Consume("global", TimeSpan.FromHours(1), now);
        }
    }

    async Task<WorkLease> ReserveLease(CancellationToken cancellationToken)
    {
        var config = options.Value;
        var limits = config.Protection;
        var exposure = new SpendExposure(
            limits.Anthropic.Cost(limits.Anthropic.MaxInputTokens, config.Answering.MaxOutputTokens),
            limits.Voyage.Cost((long)limits.Voyage.MaxInputTokens * (config.Voyage.MaxRetries + 1)));
        CancellationTokenSource? deadline = null;
        CancellationTokenSource? linked = null;
        try
        {
            deadline = new(TimeSpan.FromSeconds(config.Discord.AnswerTimeoutSeconds), clock);
            linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
            var id = await ledger.Reserve(exposure, linked.Token) ?? throw Refused("budget");
            var lease = new WorkLease(ledger, id, exposure, linked, deadline, () => _slots.Release());
            linked = null;
            deadline = null;
            return lease;
        }
        catch (Exception error) when (error is NpgsqlException or TimeoutException)
        {
            throw Refused("budget");
        }
        finally
        {
            linked?.Dispose();
            deadline?.Dispose();
        }
    }
}
