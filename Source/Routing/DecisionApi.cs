// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using System.Text.Json;

namespace Cratis.Prompter.Routing;

/// <summary>Thin Decision API v1 adapter with one bounded HTTP response.</summary>
/// <param name="client">Dedicated redirect-free client.</param>
/// <param name="options">Private origin and confidence thresholds.</param>
internal sealed class DecisionApi(HttpClient client, RoutingOptions options)
{
    const int MaximumResponseBytes = 64 * 1024;

    /// <summary>Issues one batch with no retries or hosted fallback.</summary>
    /// <param name="excerpt">Bounded untrusted context.</param>
    /// <param name="decisions">Caller-owned choice sets.</param>
    /// <param name="baseline">Safe fallback.</param>
    /// <param name="cancellationToken">Linked classifier deadline.</param>
    /// <returns>Validated advisory data or fallback.</returns>
    public async Task<Classification> Decide(string excerpt, IReadOnlyList<DecisionChoices> decisions, Classification baseline, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(options.Endpoint!, "/v1/decisions/batch"))
        {
            Content = JsonContent.Create(new
            {
                decisions = decisions.Select(decision => new
                {
                    requestId = decision.RequestId,
                    context = new { text = excerpt, structured = (object?)null },
                    choices = decision.Keys,
                    question = decision.Question,
                    descriptions = decision.Descriptions
                })
            })
        };
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode) return baseline with { Fallback = ClassificationFallback.Unavailable };
        if (response.Content.Headers.ContentLength > MaximumResponseBytes) return baseline with { Fallback = ClassificationFallback.InvalidResponse };
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var buffer = new byte[MaximumResponseBytes + 1];
        var count = 0;
        while (count < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(count), cancellationToken);
            if (read == 0) break;
            count += read;
        }
        if (count > MaximumResponseBytes) return baseline with { Fallback = ClassificationFallback.InvalidResponse };
        using var document = JsonDocument.Parse(buffer.AsMemory(0, count));
        return DecisionResponse.Parse(document.RootElement, decisions, options, baseline);
    }
}
