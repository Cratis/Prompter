// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Operations;

/// <summary>
/// A process-local single-flight guard for HTTP triggers, additional to the indexer's database-wide lease.
/// A full index takes minutes, so a second local trigger is rejected rather than piled on. CLI, other replicas
/// and Jobs are serialized by the Postgres lease, not by this process-local gate.
/// </summary>
public sealed class ReindexGate
{
    int _running;

    /// <summary>
    /// Attempts to claim the single re-index slot.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when the caller acquired the slot and must eventually call <see cref="Exit"/>;
    /// <see langword="false"/> when a run is already in progress.
    /// </returns>
    public bool TryEnter() => Interlocked.CompareExchange(ref _running, 1, 0) == 0;

    /// <summary>
    /// Releases the re-index slot so the next trigger may enter.
    /// </summary>
    public void Exit() => Interlocked.Exchange(ref _running, 0);
}
