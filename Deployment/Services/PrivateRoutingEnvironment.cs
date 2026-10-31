// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Prompter.Deployment.Services;

/// <summary>Projects validated non-secret routing settings without deployment resources.</summary>
public static class PrivateRoutingEnvironment
{
    /// <summary>Creates explicit runtime settings; absent endpoint leaves remote routing off.</summary>
    /// <param name="args">The private routing identity.</param>
    /// <returns>The routing-only environment values.</returns>
    /// <exception cref="InvalidPrivateRouting">Configured identity or mode is invalid.</exception>
    public static IReadOnlyDictionary<string, string> Values(PrivateRoutingArgs args)
    {
        if (!args.IsValid) throw new InvalidPrivateRouting();
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Cratis__Prompter__Routing__Endpoint"] = args.IsConfigured ? args.Endpoint : string.Empty,
            ["Cratis__Prompter__Routing__AllowedHost"] = args.IsConfigured ? args.AllowedHost : string.Empty,
            ["Cratis__Prompter__Routing__Mode"] = args.IsConfigured ? args.Mode : "Active",
            ["Cratis__Prompter__Routing__ShadowPercentage"] = "0",
            ["Cratis__Prompter__Routing__MinimumConfidence"] = args.MinimumConfidence.ToString(CultureInfo.InvariantCulture),
            ["Cratis__Prompter__Routing__MinimumMargin"] = args.MinimumMargin.ToString(CultureInfo.InvariantCulture)
        };
    }
}
