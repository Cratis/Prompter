// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Prompter.Routing;

/// <summary>Opt-in composition; does not edit host wiring or probe a service.</summary>
public static class RoutingRegistration
{
    /// <summary>Registers one shared concurrency boundary and a dedicated redirect-free private client.</summary>
    /// <param name="services">The application services.</param>
    /// <param name="options">Configuration bound from a routing-only section by the host.</param>
    /// <param name="products">Keys of GitHubOptions.Repositories, not model-provided repositories.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddRequestRouting(this IServiceCollection services, RoutingOptions options, IReadOnlyList<string> products)
    {
        services.TryAddSingleton<IClassificationSink, NoOpClassificationSink>();
        services.AddHttpClient("private-laya", client => client.Timeout = Timeout.InfiniteTimeSpan)
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                UseProxy = false,
                ConnectTimeout = TimeSpan.FromSeconds(1)
            });
        services.AddSingleton<IRequestClassifier>(provider => new LayaClassifier(
            provider.GetRequiredService<IHttpClientFactory>().CreateClient("private-laya"),
            options,
            products.ToArray(),
            provider.GetRequiredService<IClassificationSink>()));
        return services;
    }
}
