// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Protection;

/// <summary>Applies the indexing kill switch before any documentation fetch.</summary>
/// <param name="inner">The ingestion pipeline.</param>
/// <param name="options">The current kill switch.</param>
public sealed class ProtectedIndexer(Indexer inner, IOptions<PrompterOptions> options) : IIndexer
{
    /// <inheritdoc/>
    public Task<IndexRun> Run(CancellationToken cancellationToken = default) => Run(new IndexRequest(), cancellationToken);

    /// <inheritdoc/>
    public Task<IndexRun> Run(IndexRequest request, CancellationToken cancellationToken = default) =>
        options.Value.Protection.IndexingEnabled ? inner.Run(request, cancellationToken) : throw new WorkRefused();
}
