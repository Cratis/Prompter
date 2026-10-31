// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Prompter.Protection;

/// <summary>Reads at most one bounded webhook payload, including bodies without Content-Length.</summary>
public static class BoundedBody
{
    /// <summary>Reads exact signature bytes without an unbounded allocation.</summary>
    /// <param name="request">The incoming request.</param>
    /// <param name="maximum">The maximum permitted bytes.</param>
    /// <param name="cancellationToken">The request cancellation.</param>
    /// <returns>The raw body, or null when oversized.</returns>
    public static async Task<byte[]?> Read(HttpRequest request, int maximum, CancellationToken cancellationToken)
    {
        if (request.ContentLength > maximum)
        {
            return null;
        }

        var payload = new byte[maximum + 1];
        var length = 0;
        while (length < payload.Length)
        {
            var count = await request.Body.ReadAsync(payload.AsMemory(length), cancellationToken);
            if (count == 0)
            {
                return payload.AsSpan(0, length).ToArray();
            }

            length += count;
        }

        return null;
    }
}
