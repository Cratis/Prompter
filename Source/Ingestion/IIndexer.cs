// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// Defines the indexer that keeps the retrieval corpus in sync with the published documentation.
/// </summary>
public interface IIndexer
{
    /// <summary>
    /// Runs a full ingestion pass: fetch pages, chunk them, embed chunks whose content changed and
    /// remove chunks whose source no longer exists.
    /// </summary>
    /// <param name="cancellationToken">Token for cancelling the operation.</param>
    /// <returns>A <see cref="IndexRun"/> summarizing the pass.</returns>
    Task<IndexRun> Run(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs an ingestion pass with explicit mode and consent, under the shared database lease.
    /// </summary>
    /// <param name="request">The authorized run mode.</param>
    /// <param name="cancellationToken">Token for cancellation.</param>
    /// <returns>The summary of the completed pass.</returns>
    Task<IndexRun> Run(IndexRequest request, CancellationToken cancellationToken = default);
}
