// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Telemetry;

namespace Cratis.Prompter.Protection;

/// <summary>A synchronous execution-context scope carrying one durably funded dispatch permit.</summary>
public sealed class ProviderAttemptScope : IDisposable
{
    static readonly AsyncLocal<ProviderAttemptScope?> _current = new();
    readonly ProviderAttemptScope? _previous;
    readonly WorkLease? _lease;
    decimal _upper;
    bool _prepaid;
    int _granted;
    int _consumed;
    int _completed;
    int _disposed;

    ProviderAttemptScope(ProviderDispatch dispatch, int inputCount)
    {
        Dispatch = dispatch;
        InputCount = inputCount;
        _lease = WorkLease.Current;
        _previous = _current.Value;
        _current.Value = this;
    }

    /// <summary>Gets the currently visible attempt.</summary>
    public static ProviderAttemptScope? Current => _current.Value;

    /// <summary>Gets the bounded metadata.</summary>
    public ProviderDispatch Dispatch { get; }

    /// <summary>Gets the embedding batch size (one for chat).</summary>
    public int InputCount { get; }

    /// <summary>Gets the currently prepaid parent, if any.</summary>
    internal Guid? Parent => _lease?.Reservation;

    /// <summary>Enters before awaiting reservation, so the granted object remains visible to inner decorators.</summary>
    /// <param name="dispatch">The exact dispatch metadata.</param>
    /// <param name="inputCount">Number of inputs.</param>
    /// <returns>A scope restoring its predecessor on disposal.</returns>
    public static ProviderAttemptScope Enter(ProviderDispatch dispatch, int inputCount = 1) => new(dispatch, inputCount);

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            _current.Value = _previous;
        }
    }

    /// <summary>Claims the only reservation slot in this scope.</summary>
    /// <param name="dispatch">The exact attempt.</param>
    /// <returns>Whether this is the first matching claim.</returns>
    internal bool BeginReservation(ProviderDispatch dispatch) =>
        _disposed == 0 && Dispatch == dispatch && ReferenceEquals(_lease, WorkLease.Current) && Interlocked.CompareExchange(ref _granted, -1, 0) == 0;

    /// <summary>Publishes funding only after the database commit succeeds.</summary>
    /// <param name="funding">Committed exposure and ownership.</param>
    internal void Grant(ProviderAttemptFunding funding)
    {
        _upper = funding.Upper;
        _prepaid = funding.Prepaid;
        Volatile.Write(ref _granted, 1);
    }

    /// <summary>Authorizes at most one matching inner transport call.</summary>
    /// <param name="request">The bounded inner request.</param>
    /// <exception cref="WorkRefused">The permit does not match or has already been consumed.</exception>
    internal void Consume(ProviderAttemptRequest request)
    {
        ValidateIdentity(request);
        if (Interlocked.CompareExchange(ref _consumed, 1, 0) != 0)
        {
            throw new WorkRefused();
        }

        if (_prepaid && _lease is not null)
        {
            if (request.Provider == "anthropic")
            {
                _lease.DispatchChat();
            }
            else
            {
                _lease.DispatchEmbedding(request.Upper);
            }
        }
    }

    /// <summary>Reconciles the live prepaid envelope only after durable completion.</summary>
    /// <param name="completion">Durable usage evidence.</param>
    internal void Completed(ProviderAttemptCompletion completion)
    {
        if (completion.Cost is null || !CanComplete())
        {
            return;
        }

        if (Dispatch.Provider == "anthropic")
        {
            _lease?.ChatUsed(completion.Cost.Value);
        }
        else
        {
            _lease?.EmbeddingUsed(_upper, completion.Cost.Value);
        }
    }

    bool CanComplete() => _prepaid && _consumed == 1 && Interlocked.Exchange(ref _completed, 1) == 0;

    void ValidateIdentity(ProviderAttemptRequest request)
    {
        ProviderAttemptPolicy.Require(_disposed == 0 && _granted == 1);
        ProviderAttemptPolicy.Require(Dispatch.Provider == request.Provider && Dispatch.Model == request.Model);
        ProviderAttemptPolicy.Require(_upper == request.Upper && InputCount == request.InputCount);
        ProviderAttemptPolicy.Require(ReferenceEquals(_lease, WorkLease.Current));
    }
}
