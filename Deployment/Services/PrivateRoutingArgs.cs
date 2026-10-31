// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Pulumi;

namespace Cratis.Prompter.Deployment.Services;

/// <summary>Explicit non-secret Laya identity shared by runtime and bot-only egress.</summary>
public sealed partial record PrivateRoutingArgs
{
    /// <summary>Gets the private service origin; absence leaves deterministic fallback.</summary>
    public string Endpoint { get; init; } = string.Empty;

    /// <summary>Gets the independently approved service host.</summary>
    public string AllowedHost { get; init; } = string.Empty;

    /// <summary>Gets the destination service name.</summary>
    public string Service { get; init; } = string.Empty;

    /// <summary>Gets the destination namespace.</summary>
    public string Namespace { get; init; } = string.Empty;

    /// <summary>Gets the destination app label.</summary>
    public string App { get; init; } = string.Empty;

    /// <summary>Gets the destination environment label.</summary>
    public string Environment { get; init; } = string.Empty;

    /// <summary>Gets the independently verified backend port, not the service port.</summary>
    public int BackendPort { get; init; }

    /// <summary>Gets the explicit Active or Shadow mode; no implicit activation.</summary>
    public string Mode { get; init; } = string.Empty;

    /// <summary>Gets the minimum winning probability.</summary>
    public double MinimumConfidence { get; init; } = 0.75;

    /// <summary>Gets the minimum winner margin.</summary>
    public double MinimumMargin { get; init; } = 0.15;

    /// <summary>Gets whether remote routing is explicitly configured.</summary>
    public bool IsConfigured => Endpoint.Length > 0;

    /// <summary>Gets whether the complete service identity and routing settings are valid.</summary>
    public bool IsValid => !IsConfigured || (IsName(Service) && IsName(Namespace) && IsName(App) && IsName(Environment)
        && BackendPort is > 0 and <= 65535
        && AllowedHost == $"{Service}.{Namespace}.svc.cluster.local"
        && (Mode == "Active" || Mode == "Shadow")
        && IsProbability(MinimumConfidence) && IsProbability(MinimumMargin)
        && IsOrigin());

    [GeneratedRegex(@"\A[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\z", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex Name { get; }

    /// <summary>Reads only explicit non-secret routing settings, without guessing destinations.</summary>
    /// <param name="config">Deployment configuration.</param>
    /// <returns>The private routing configuration.</returns>
    public static PrivateRoutingArgs From(Config config) => new()
    {
        Endpoint = Setting(config, "layaEndpoint"),
        AllowedHost = Setting(config, "layaAllowedHost"),
        Service = Setting(config, "layaService"),
        Namespace = Setting(config, "layaNamespace"),
        App = Setting(config, "layaApp"),
        Environment = Setting(config, "layaEnvironment"),
        BackendPort = config.GetInt32("layaBackendPort") ?? 0,
        Mode = Setting(config, "layaMode")
    };

    static string Setting(Config config, string name) => config.Get(name) ?? string.Empty;

    static bool IsName(string value) => Name.IsMatch(value);

    static bool IsProbability(double value) => double.IsFinite(value) && value is >= 0 and <= 1;

    bool IsOrigin() => Uri.TryCreate(Endpoint, UriKind.Absolute, out var endpoint)
        && (endpoint.Scheme == "http" || endpoint.Scheme == "https") && endpoint.Port is > 0 and <= 65535
        && endpoint.DnsSafeHost == AllowedHost && HasNoOriginExtras(endpoint)
        && IsCanonicalOrigin(endpoint);

    static bool HasNoOriginExtras(Uri endpoint) => endpoint.UserInfo.Length == 0
        && endpoint.AbsolutePath == "/" && endpoint.Query.Length == 0 && endpoint.Fragment.Length == 0;

    bool IsCanonicalOrigin(Uri endpoint) => Endpoint.TrimEnd('/') == $"{endpoint.Scheme}://{AllowedHost}:{endpoint.Port}"
        || Endpoint.TrimEnd('/') == $"{endpoint.Scheme}://{AllowedHost}";
}
