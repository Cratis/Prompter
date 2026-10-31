// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Deployment.Services;

/// <summary>Projects only explicitly validated, non-secret metrics settings.</summary>
public static class PrivateMetricsEnvironment
{
    /// <summary>Creates runtime environment values without constructing deployment resources.</summary>
    /// <param name="args">The collector identity.</param>
    /// <param name="environment">The source workload environment.</param>
    /// <returns>The explicit metrics configuration.</returns>
    /// <exception cref="InvalidPrivateMetrics">Enabled configuration is not approved.</exception>
    public static IReadOnlyDictionary<string, string> Values(PrivateMetricsArgs args, string environment)
    {
        if (!args.IsValid) throw new InvalidPrivateMetrics();
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Cratis__Prompter__Metrics__Enabled"] = args.Enabled ? "true" : "false",
            ["Cratis__Prompter__Metrics__Environment"] = environment,
            ["Cratis__Prompter__Metrics__Endpoint"] = args.Enabled ? args.Endpoint : string.Empty,
            ["Cratis__Prompter__Metrics__AllowedHost"] = args.Enabled ? args.AllowedHost : string.Empty
        };
    }
}
