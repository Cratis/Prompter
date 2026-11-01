// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// Reads successful responses without trusting their declared length.
/// </summary>
public static class BoundedContent
{
    /// <summary>The longest time a response body may take to arrive.</summary>
    public static readonly TimeSpan ReadTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Reads a bounded UTF-8 response.</summary>
    /// <param name="response">The response to read.</param>
    /// <param name="maxBytes">The maximum accepted bytes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response text.</returns>
    /// <exception cref="IncompleteCrawl">The response failed, exceeded the bound or did not arrive within <see cref="ReadTimeout"/>.</exception>
    public static Task<string> Read(HttpResponseMessage response, int maxBytes, CancellationToken cancellationToken) =>
        Read(response, maxBytes, ReadTimeout, cancellationToken);

    /// <summary>Reads a bounded UTF-8 response within a deadline.</summary>
    /// <param name="response">The response to read.</param>
    /// <param name="maxBytes">The maximum accepted bytes.</param>
    /// <param name="timeout">The longest time the body may take to arrive.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response text.</returns>
    /// <exception cref="IncompleteCrawl">The response failed, exceeded the bound or did not arrive in time.</exception>
    public static async Task<string> Read(HttpResponseMessage response, int maxBytes, TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength > maxBytes) throw new IncompleteCrawl();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        try
        {
            return await ReadBody(response, maxBytes, deadline.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new IncompleteCrawl();
        }
    }

    /// <summary>Checks the response media type without depending on its charset.</summary>
    /// <param name="response">The response.</param>
    /// <param name="types">The accepted media types.</param>
    /// <returns>Whether the response has an accepted media type.</returns>
    public static bool MediaType(HttpResponseMessage response, params string[] types) =>
        types.Contains(response.Content.Headers.ContentType?.MediaType, StringComparer.OrdinalIgnoreCase);

    static async Task<string> ReadBody(HttpResponseMessage response, int maxBytes, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var content = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (content.Length + count > maxBytes) throw new IncompleteCrawl();
            await content.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
        }
        return Encoding.UTF8.GetString(content.GetBuffer(), 0, (int)content.Length);
    }
}
