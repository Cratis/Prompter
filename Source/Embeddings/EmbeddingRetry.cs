// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Prompter.Embeddings;

/// <summary>
/// Pure retry policy for embedding generation: decides which failures are worth retrying and how long to
/// back off between attempts. Kept free of I/O so it can be specified in isolation.
/// </summary>
public static class EmbeddingRetry
{
    /// <summary>
    /// Determines whether a failed embedding request should be retried, based on its HTTP status. Rate
    /// limiting (429) and server errors (5xx) are transient, and so is a missing status: a request that never
    /// got an HTTP response - a connection reset, a DNS blip or a socket timeout - surfaces as an
    /// <see cref="HttpRequestException"/> with no <see cref="HttpStatusCode"/>, and those network faults are
    /// common on long index runs and worth retrying. Client errors that did get a status (a bad request or an
    /// invalid key) are not.
    /// </summary>
    /// <param name="status">The HTTP status of the failed response, or <see langword="null"/> when the request never got one.</param>
    /// <returns><see langword="true"/> when the failure is transient and should be retried; otherwise <see langword="false"/>.</returns>
    public static bool IsTransient(HttpStatusCode? status) =>
        status is null or HttpStatusCode.TooManyRequests or (>= HttpStatusCode.InternalServerError and < (HttpStatusCode)600);

    /// <summary>
    /// Computes the delay before the given retry attempt using exponential backoff, doubling the base delay
    /// for each successive attempt (attempt 0 waits the base delay, attempt 1 twice that, and so on).
    /// </summary>
    /// <param name="attempt">The zero-based retry attempt.</param>
    /// <param name="baseDelay">The base delay for the first attempt.</param>
    /// <returns>The delay to wait before the attempt.</returns>
    public static TimeSpan BackoffFor(int attempt, TimeSpan baseDelay) =>
        attempt <= 0 ? baseDelay : baseDelay * Math.Pow(2, attempt);
}
