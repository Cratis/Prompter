// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Telemetry;

/// <summary>Private metrics only; exporting is opt-in and never starts a listener.</summary>
public sealed class MetricsOptions
{
    /// <summary>Gets or sets whether to export aggregate metrics to the cluster collector.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the deployment environment; only fixed operational values are exported.
    /// </summary>
    public string Environment { get; set; } = "unknown";
}
