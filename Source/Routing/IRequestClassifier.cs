// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Routing;

/// <summary>Request-scoped advisory classification after admission and sanitization.</summary>
public interface IRequestClassifier
{
    /// <summary>Classifies text without authorizing effects or changing trusted prompts.</summary>
    /// <param name="currentRequest">Sanitized current request, without identities.</param>
    /// <param name="olderRequests">Already bounded, sanitized same-public-scope request excerpts only.</param>
    /// <param name="cancellationToken">The admitted request/lease cancellation.</param>
    /// <returns>Allowlisted advisory data or deterministic safe fallback.</returns>
    Task<Classification> Classify(string currentRequest, IReadOnlyList<string> olderRequests, CancellationToken cancellationToken = default);
}
