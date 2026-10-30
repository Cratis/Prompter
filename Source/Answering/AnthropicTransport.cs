// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Prompter.Embeddings;

namespace Cratis.Prompter.Answering;

/// <summary>
/// Prevents SDK error bodies from reaching logs and classifies only verified HTTP overload statuses.
/// </summary>
public sealed class AnthropicTransport : DelegatingHandler
{
    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        using (response)
        {
            if (response.StatusCode is HttpStatusCode.ServiceUnavailable or (HttpStatusCode)529)
            {
                var hint = response.Headers.TryGetValues("Retry-After", out var values) ? values.FirstOrDefault() : null;
                throw new AnthropicCapacityFailure(EmbeddingRetry.RetryAfter(hint, DateTimeOffset.UtcNow));
            }

            throw new ProviderUnavailable("nonretryable_or_unknown");
        }
    }
}
