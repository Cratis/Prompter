// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// Holds the database-wide index slot until disposed. Reservations survive process failure.
/// </summary>
public interface IIndexLease : IAsyncDisposable
{
    /// <summary>Rejects an incremental run that would mix embedding profiles.</summary>
    /// <param name="profile">The model, dimension and chunking version.</param>
    /// <param name="full">Whether a confirmed full replacement is authorized.</param>
    /// <param name="cancellationToken">Token for cancellation.</param>
    /// <returns>Awaitable task.</returns>
    Task CheckProfile(string profile, bool full, CancellationToken cancellationToken);

    /// <summary>Checks the owning session before another provider dispatch.</summary>
    /// <param name="cancellationToken">Token for cancellation.</param>
    /// <returns>Awaitable task.</returns>
    Task EnsureHeld(CancellationToken cancellationToken);

    /// <summary>Atomically publishes a complete run on the owning session.</summary>
    /// <param name="changed">All staged changes.</param>
    /// <param name="existing">All identifiers discovered in the complete crawl.</param>
    /// <param name="cancellationToken">Token for cancellation.</param>
    /// <returns>The number of stale chunks removed.</returns>
    Task<int> CommitIndex(IReadOnlyCollection<EmbeddedChunk> changed, IReadOnlySet<ChunkId> existing, CancellationToken cancellationToken);

    /// <summary>
    /// Reserves retry-inclusive exposure and, for scheduled work, the single daily slot.
    /// Reservations are deliberately not refunded after an unknown provider outcome.
    /// </summary>
    /// <param name="exposure">The conservative token and dollar exposure.</param>
    /// <param name="dailyLimit">The configured daily dollar ceiling.</param>
    /// <param name="scheduled">Whether this is scheduled work.</param>
    /// <param name="cancellationToken">Token for cancellation.</param>
    /// <returns>Awaitable task; rejection throws before dispatch.</returns>
    Task Reserve(IndexExposure exposure, decimal dailyLimit, bool scheduled, CancellationToken cancellationToken);
}
