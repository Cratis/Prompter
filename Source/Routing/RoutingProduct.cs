// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Routing;

/// <summary>A configured routing-map key, not a repository or a provider-selected URL.</summary>
/// <param name="Value">The allowlisted key, or unknown.</param>
public sealed record RoutingProduct(string Value) : ConceptAs<string>(Value)
{
    /// <summary>Gets the deliberately unrouted product.</summary>
    public static RoutingProduct Unknown { get; } = new("unknown");
}
