// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Npgsql;

namespace Cratis.Prompter.Protection;

/// <summary>A request's reserved exposure, deadline, and slot. Unknown dispatches are never refunded.</summary>
/// <param name="ledger">The durable ledger.</param>
/// <param name="id">The reservation identity.</param>
/// <param name="reserved">The worst-case exposure.</param>
/// <param name="linked">The request cancellation source.</param>
/// <param name="deadline">The deadline source.</param>
/// <param name="release">The concurrency release action.</param>
public sealed class WorkLease(
    ISpendLedger ledger,
    Guid id,
    SpendExposure reserved,
    CancellationTokenSource linked,
    CancellationTokenSource deadline,
    Action release) : IAsyncDisposable
{
    static readonly AsyncLocal<WorkLease?> _current = new();
    readonly Lock _lock = new();
    decimal _chat;
    decimal _embedding;
    bool _chatDispatched;
    bool _chatKnown;
    int _disposed;

    /// <summary>Gets the active execution-context lease.</summary>
    public static WorkLease? Current => _current.Value is { _disposed: 0 } lease ? lease : null;

    /// <summary>Gets the shared deadline token.</summary>
    public CancellationToken Token => linked.Token;

    /// <summary>Gets the durable parent funding identifier.</summary>
    internal Guid Reservation => id;

    /// <summary>Activates this lease in the caller execution context before invoking dependencies.</summary>
    public void Activate() => _current.Value = this;

    /// <summary>Marks a chat dispatch before sending it.</summary>
    /// <exception cref="WorkRefused">An additional unreserved dispatch was attempted.</exception>
    public void DispatchChat()
    {
        Token.ThrowIfCancellationRequested();
        lock (_lock)
        {
            if (_chatDispatched || _disposed != 0)
            {
                throw new WorkRefused();
            }

            _chatDispatched = true;
        }
    }

    /// <summary>Records a known upper-cost chat result.</summary>
    /// <param name="cost">The billed token exposure.</param>
    public void ChatUsed(decimal cost)
    {
        lock (_lock)
        {
            if (_chatDispatched && WithinChatReserve(cost))
            {
                _chat = cost;
                _chatKnown = true;
            }
        }
    }

    /// <summary>Charges an embedding attempt before sending it, including ambiguous failures.</summary>
    /// <param name="cost">The bounded attempt exposure.</param>
    /// <exception cref="WorkRefused">The dispatch would exceed reserved exposure.</exception>
    public void DispatchEmbedding(decimal cost)
    {
        Token.ThrowIfCancellationRequested();
        lock (_lock)
        {
            ProviderAttemptPolicy.Require(_disposed == 0);
            if (cost <= 0 || _embedding + cost > reserved.Voyage)
            {
                throw new WorkRefused();
            }

            _embedding += cost;
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        if (ReferenceEquals(_current.Value, this))
        {
            _current.Value = null;
        }

        try
        {
            // Cleanup has its own short deadline even after the request has been canceled.
            using var settlement = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            SpendExposure exposure;
            lock (_lock)
            {
                exposure = new(_chatDispatched && !_chatKnown ? reserved.Anthropic : _chat, _embedding);
            }

            await ledger.Reconcile(id, exposure, settlement.Token);
        }
        catch (Exception error) when (error is NpgsqlException or TimeoutException or OperationCanceledException)
        {
            // The durable reservation remains intact. Never refund an unknown outcome on a retry or restart.
        }
        finally
        {
            linked.Dispose();
            deadline.Dispose();
            release();
        }
    }

    /// <summary>Replaces one completed embedding bound with known usage.</summary>
    /// <param name="upper">The dispatched upper bound.</param>
    /// <param name="cost">The durably known usage.</param>
    internal void EmbeddingUsed(decimal upper, decimal cost)
    {
        lock (_lock)
        {
            if (cost >= 0 && cost <= upper)
            {
                _embedding -= upper - cost;
            }
        }
    }

    bool WithinChatReserve(decimal cost) => cost >= 0 && cost <= reserved.Anthropic;
}
