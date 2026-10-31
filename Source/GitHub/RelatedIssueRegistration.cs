// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub.Authentication;
using Cratis.Prompter.Protection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Prompter.GitHub;

/// <summary>Isolated GET-only discovery using the same explicit installation authorization as issue operations.</summary>
public static class RelatedIssueRegistration
{
    /// <summary>Registers discovery without requiring credentials at startup or adding retry policies.</summary>
    /// <param name="services">The host services, including GitHubWriteGate.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddRelatedIssueSearch(this IServiceCollection services)
    {
        services.AddHttpClient<IRelatedIssues, RelatedIssues>(client =>
        {
            client.Timeout = Timeout.InfiniteTimeSpan;
            client.DefaultRequestHeaders.Accept.Add(new("application/vnd.github+json"));
            client.DefaultRequestHeaders.UserAgent.Add(new("Cratis-Prompter", "1.0"));
        }).AddHttpMessageHandler<GitHubWriteGate>().AddGitHubAppAuthentication();
        services.AddSingleton<PublicIssueSearchLimits>();
        services.AddHttpClient<IPublicRelatedIssues, PublicRelatedIssues>(client =>
        {
            client.BaseAddress = new("https://api.github.com/");
            client.Timeout = Timeout.InfiniteTimeSpan;
            client.DefaultRequestHeaders.Accept.Add(new("application/vnd.github+json"));
            client.DefaultRequestHeaders.UserAgent.Add(new("Cratis-Prompter", "1.0"));
        }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false });
        return services;
    }
}
