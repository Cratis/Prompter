// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Routing;

/// <summary>Private Decision API configuration. No endpoint means deterministic fallback.</summary>
public sealed class RoutingOptions
{
    /// <summary>Gets or sets the explicitly approved private service origin (no path/query).</summary>
    public Uri? Endpoint { get; set; }

    /// <summary>Gets or sets the exact approved private host, independently of the endpoint.</summary>
    public string AllowedHost { get; set; } = string.Empty;

    /// <summary>Gets or sets the mode. Active uses configured Laya; shadow is opt-in.</summary>
    public ClassificationMode Mode { get; set; }

    /// <summary>Gets or sets the deterministic shadow percentage, clamped to 0–10; default off.</summary>
    public int ShadowPercentage { get; set; }

    /// <summary>Gets or sets the unvalidated minimum winning probability (0.75 by default).</summary>
    public double MinimumConfidence { get; set; } = 0.75;

    /// <summary>Gets or sets the unvalidated minimum winner margin (0.15 by default).</summary>
    public double MinimumMargin { get; set; } = 0.15;
}
