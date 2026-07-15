// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Storage;

/// <summary>
/// Defines the stored chunks that make up the retrieval corpus.
/// </summary>
public interface IChunks
{
    /// <summary>
    /// Ensures the database schema exists.
    /// </summary>
    /// <param name="cancellationToken">Token for cancelling the operation.</param>
    /// <returns>Awaitable task.</returns>
    Task EnsureSchema(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the content hash of every stored chunk, keyed by chunk identifier.
    /// </summary>
    /// <param name="cancellationToken">Token for cancelling the operation.</param>
    /// <returns>The hashes keyed by <see cref="ChunkId"/>.</returns>
    Task<IReadOnlyDictionary<ChunkId, ContentHash>> GetHashes(CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or updates a chunk with its embedding.
    /// </summary>
    /// <param name="chunk">The chunk to upsert.</param>
    /// <param name="embedding">The embedding vector for the chunk.</param>
    /// <param name="cancellationToken">Token for cancelling the operation.</param>
    /// <returns>Awaitable task.</returns>
    Task Upsert(Chunk chunk, ReadOnlyMemory<float> embedding, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes every chunk whose identifier is not in the given set of chunks that still exist at the source.
    /// </summary>
    /// <param name="existing">The identifiers of chunks that still exist.</param>
    /// <param name="cancellationToken">Token for cancelling the operation.</param>
    /// <returns>The number of chunks removed.</returns>
    Task<int> RemoveAllExcept(IReadOnlySet<ChunkId> existing, CancellationToken cancellationToken = default);
}
