// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Prompter.GitHub;

/// <summary>Bounded, credential-free HTTP helpers for anonymous public search.</summary>
internal static class PublicSearchHttp
{
    const int MaximumResponseBytes = 128 * 1024;

    /// <summary>Reads the server-provided wait from Retry-After or the rate-limit reset header.</summary>
    /// <param name="response">The rate-limit response.</param>
    /// <param name="now">The current time.</param>
    /// <returns>The wait, or <see langword="null"/> when absent or unusable.</returns>
    internal static TimeSpan? RetryAfter(HttpResponseMessage response, DateTimeOffset now)
    {
        if (response.Headers.RetryAfter?.Delta is { } delta)
        {
            return delta;
        }

        return response.Headers.TryGetValues("x-ratelimit-reset", out var values) &&
            long.TryParse(values.FirstOrDefault(), NumberStyles.None, CultureInfo.InvariantCulture, out var reset) &&
            reset is > 0 and < 253402300799
                ? DateTimeOffset.FromUnixTimeSeconds(reset) - now
                : null;
    }

    /// <summary>Reads a response body only while it stays within the size bound.</summary>
    /// <param name="response">The response.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The bytes, or <see langword="null"/> when oversized.</returns>
    internal static async Task<byte[]?> Bounded(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength > MaximumResponseBytes)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var bounded = new MemoryStream();
        var buffer = new byte[4096];
        int read;
        while ((read = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (bounded.Length + read > MaximumResponseBytes)
            {
                return null;
            }

            await bounded.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return bounded.ToArray();
    }
}
