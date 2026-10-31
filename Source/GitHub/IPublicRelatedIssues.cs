// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.GitHub;

/// <summary>Anonymous, read-only discovery of public related issues; never required for answering.</summary>
public interface IPublicRelatedIssues
{
    /// <summary>Looks up public issues in the fixed Cratis repositories using current-request keywords only.</summary>
    /// <param name="currentRequest">The current request, without history or identity metadata.</param>
    /// <param name="cancellationToken">Caller cancellation, which is propagated rather than swallowed.</param>
    /// <returns>At most three validated hints, open first; empty on any failure or when disabled.</returns>
    Task<IReadOnlyList<RelatedIssue>> Find(string currentRequest, CancellationToken cancellationToken = default);
}
