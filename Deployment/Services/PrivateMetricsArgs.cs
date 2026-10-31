// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Pulumi;

namespace Cratis.Prompter.Deployment.Services;

/// <summary>Explicit non-secret collector identity shared by runtime and egress projection.</summary>
public sealed partial record PrivateMetricsArgs
{
    /// <summary>Gets whether private export is enabled.</summary>
    public bool Enabled { get; init; }

    /// <summary>Gets the exact OTLP metrics endpoint.</summary>
    public string Endpoint { get; init; } = string.Empty;

    /// <summary>Gets the independently approved service host.</summary>
    public string AllowedHost { get; init; } = string.Empty;

    /// <summary>Gets the destination service name.</summary>
    public string Service { get; init; } = string.Empty;

    /// <summary>Gets the destination namespace name.</summary>
    public string Namespace { get; init; } = string.Empty;

    /// <summary>Gets the destination app label.</summary>
    public string App { get; init; } = string.Empty;

    /// <summary>Gets the destination environment label.</summary>
    public string Environment { get; init; } = string.Empty;

    /// <summary>Gets the explicitly verified backend port.</summary>
    public int BackendPort { get; init; }

    /// <summary>Gets whether the complete collector identity is approved.</summary>
    public bool IsValid => !Enabled || (IsName(Service) && IsName(Namespace) && IsName(App) && IsName(Environment)
        && BackendPort is > 0 and <= 65535
        && AllowedHost == $"{Service}.{Namespace}.svc.cluster.local"
        && Endpoint == $"http://{AllowedHost}:4318/v1/metrics"
        && Uri.TryCreate(Endpoint, UriKind.Absolute, out var endpoint)
        && endpoint.Scheme == "http" && endpoint.Port == 4318 && endpoint.AbsolutePath == "/v1/metrics"
        && endpoint.DnsSafeHost == AllowedHost && endpoint.UserInfo.Length == 0
        && endpoint.Query.Length == 0 && endpoint.Fragment.Length == 0);

    [GeneratedRegex(@"\A[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\z", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex Name { get; }

    /// <summary>Reads only explicit non-secret collector settings; no guessed destinations.</summary>
    /// <param name="config">Deployment configuration.</param>
    /// <returns>The private collector configuration.</returns>
    public static PrivateMetricsArgs From(Config config) => new()
    {
        Enabled = config.GetBoolean("metricsEnabled") ?? false,
        Endpoint = Setting(config, "metricsEndpoint"),
        AllowedHost = Setting(config, "metricsAllowedHost"),
        Service = Setting(config, "metricsService"),
        Namespace = Setting(config, "metricsNamespace"),
        App = Setting(config, "metricsApp"),
        Environment = Setting(config, "metricsEnvironment"),
        BackendPort = config.GetInt32("metricsBackendPort") ?? 0
    };

    static string Setting(Config config, string name) => config.Get(name) ?? string.Empty;

    static bool IsName(string value) => Name.IsMatch(value);
}
