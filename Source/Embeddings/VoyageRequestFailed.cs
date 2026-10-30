// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Prompter.Embeddings;

/// <summary>
/// A sanitized Voyage failure carrying retry metadata, never the response body.
/// </summary>
/// <param name="status">The HTTP status.</param>
/// <param name="retryAfter">The provider's minimum retry delay.</param>
public sealed class VoyageRequestFailed(HttpStatusCode status, TimeSpan? retryAfter)
    : HttpRequestException("Voyage embedding request failed.", null, status)
{
    /// <summary>
    /// Gets the provider's minimum retry delay, if supplied.
    /// </summary>
    public TimeSpan? RetryAfter { get; } = retryAfter;
}
