// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Cratis.Prompter.GitHub.Authentication;

/// <summary>
/// Opt-in wiring; does not remove or exempt the protected GitHub write gate.
/// </summary>
public static class GitHubAuthenticationRegistration
{
    const string MintClient = "Prompter.GitHub.AppTokenMint";

    /// <summary>
    /// Adds credential isolation and authorization after the existing outer write gate.
    /// </summary>
    /// <param name="builder">The issues client, already protected by GitHubWriteGate.</param>
    /// <returns>The same client builder.</returns>
    /// <remarks>Remove legacy default bearer authorization in the host; this handler supplies it per request.</remarks>
    public static IHttpClientBuilder AddGitHubAppAuthentication(this IHttpClientBuilder builder)
    {
        var services = builder.Services;
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<GitHubPolicy>();
        services.TryAddSingleton<IAppKey, FileAppKey>();
        services.TryAddTransient<GitHubAuthentication>();
        services.AddHttpClient(MintClient).ConfigurePrimaryHttpMessageHandler(NoRedirects).RemoveAllLoggers();
        services.TryAddSingleton(provider => new AppTokens(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient(MintClient),
            provider.GetRequiredService<IAppKey>(),
            provider.GetRequiredService<IOptions<PrompterOptions>>(),
            provider.GetRequiredService<TimeProvider>()));
        return builder.ConfigureHttpClient(client =>
        {
            client.BaseAddress = new("https://api.github.com/");
            client.DefaultRequestHeaders.Authorization = null;
        }).ConfigurePrimaryHttpMessageHandler(NoRedirects).AddHttpMessageHandler<GitHubAuthentication>().RemoveAllLoggers();
    }

    static HttpMessageHandler NoRedirects() => new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false, UseDefaultCredentials = false };
}
