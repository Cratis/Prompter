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
    decimal _chat;
    decimal _embedding;
    bool _chatDispatched;
    bool _chatKnown;
    int _disposed;

    /// <summary>Gets the active execution-context lease.</summary>
    public static WorkLease? Current => _current.Value is { _disposed: 0 } lease ? lease : null;

    /// <summary>Gets the shared deadline token.</summary>
    public CancellationToken Token => linked.Token;

    /// <summary>Activates this lease in the caller execution context before invoking dependencies.</summary>
    public void Activate() => _current.Value = this;

    /// <summary>Marks a chat dispatch before sending it.</summary>
    /// <exception cref="WorkRefused">An additional unreserved dispatch was attempted.</exception>
    public void DispatchChat()
    {
        Token.ThrowIfCancellationRequested();
        if (_chatDispatched)
        {
            throw new WorkRefused();
        }

        _chatDispatched = true;
    }

    /// <summary>Records a known upper-cost chat result.</summary>
    /// <param name="cost">The billed token exposure.</param>
    public void ChatUsed(decimal cost)
    {
        _chat = cost;
        _chatKnown = true;
    }

    /// <summary>Charges an embedding attempt before sending it, including ambiguous failures.</summary>
    /// <param name="cost">The bounded attempt exposure.</param>
    /// <exception cref="WorkRefused">The dispatch would exceed reserved exposure.</exception>
    public void DispatchEmbedding(decimal cost)
    {
        Token.ThrowIfCancellationRequested();
        if (_embedding + cost > reserved.Voyage)
        {
            throw new WorkRefused();
        }

        _embedding += cost;
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
            // Cleanup cannot extend the request deadline; a canceled settlement retains the durable reserve.
            using var settlement = CancellationTokenSource.CreateLinkedTokenSource(Token);
            settlement.CancelAfter(TimeSpan.FromSeconds(5));
            await ledger.Reconcile(id, new(_chatDispatched && !_chatKnown ? reserved.Anthropic : _chat, _embedding), settlement.Token);
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
}
