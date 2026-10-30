// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.GitHub;

/// <summary>Read-only advisory discovery; has no filing or commenting surface.</summary>
public interface IRelatedIssues
{
    /// <summary>Looks up an allowlisted product using current-request keywords only.</summary>
    /// <param name="product">A configured product key, never a repository supplied by a model.</param>
    /// <param name="currentRequest">Sanitized current request, without history or identity metadata.</param>
    /// <param name="cancellationToken">The admitted request cancellation.</param>
    /// <returns>At most six hints, open first; empty if unavailable.</returns>
    Task<IReadOnlyList<RelatedIssue>> FindRelated(string product, string currentRequest, CancellationToken cancellationToken = default);
}
