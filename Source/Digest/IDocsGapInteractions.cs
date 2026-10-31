// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Digest;

/// <summary>
/// Reads existing metadata for the weekly maintainer digest.
/// </summary>
public interface IDocsGapInteractions
{
    /// <summary>
    /// Reads gap candidates and negative feedback in a half-open UTC interval.
    /// </summary>
    /// <param name="from">The inclusive start.</param>
    /// <param name="to">The exclusive end.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>At most 10,000 metadata rows.</returns>
    Task<IReadOnlyList<DocsGapRow>> Read(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);
}
