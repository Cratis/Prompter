// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Prompter.GitHub.Authentication;

/// <summary>
/// Attaches installation authorization only to approved API routes, with no request replay.
/// </summary>
/// <param name="policy">The pinned destination and credential policy.</param>
/// <param name="tokens">The isolated installation token cache, absent for fallback-only configurations.</param>
public sealed class GitHubAuthentication(GitHubPolicy policy, AppTokens? tokens) : DelegatingHandler
{
    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        policy.RequireRequest(request);
        var token = policy.HasApp
            ? await (tokens ?? throw new GitHubAuthorizationUnavailable()).Get(cancellationToken)
            : policy.Fallback();
        request.Headers.Authorization = new("Bearer", token);
        request.Headers.Remove("X-GitHub-Api-Version");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        var response = await base.SendAsync(request, cancellationToken);
        if (policy.HasApp && response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Only a later, separately authorized operation can obtain a fresh token. Never replay POST.
            await tokens!.Invalidate(token, CancellationToken.None);
        }
        return response;
    }
}
