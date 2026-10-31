// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Prompter.Protection;

/// <summary>Applies the GitHub write kill switch at the outbound HTTP boundary.</summary>
/// <param name="options">The current switch.</param>
public sealed class GitHubWriteGate(IOptions<PrompterOptions> options) : DelegatingHandler
{
    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method != HttpMethod.Get && !options.Value.Protection.GitHubWritesEnabled)
        {
            throw new WorkRefused();
        }

        return base.SendAsync(request, cancellationToken);
    }
}
