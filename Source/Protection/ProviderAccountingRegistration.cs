// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Telemetry;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Prompter.Protection;

/// <summary>Production composition for authoritative provider attempt accounting.</summary>
public static class ProviderAccountingRegistration
{
    /// <summary>Registers the real durable store and accounting seam.</summary>
    /// <param name="services">Application services with the shared Npgsql data source.</param>
    /// <returns>The services for composition.</returns>
    public static IServiceCollection AddProviderAccounting(this IServiceCollection services)
    {
        services.AddSingleton<IProviderAttemptStore, ProviderAttemptStore>();
        services.AddSingleton<IProviderAccounting, ProviderAccounting>();
        return services;
    }
}
