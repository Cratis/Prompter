// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Cratis.Prompter.Telemetry;

/// <summary>Validates an independently approved collector without DNS or network probes.</summary>
public static partial class PrivateMetricsEndpoint
{
    [GeneratedRegex(@"\A[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.svc\.cluster\.local\z", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex ServiceHost { get; }

    /// <summary>Checks the exact cluster service, OTLP port and metrics path.</summary>
    /// <param name="options">Explicit metrics configuration.</param>
    /// <returns>Whether the endpoint is approved for metrics export.</returns>
    public static bool IsAllowed(MetricsOptions options) => options.Endpoint is { IsAbsoluteUri: true } endpoint
        && ServiceHost.IsMatch(options.AllowedHost)
        && string.Equals(endpoint.DnsSafeHost, options.AllowedHost, StringComparison.Ordinal)
        && IsMetricsUri(endpoint, options.AllowedHost);

    static bool IsMetricsUri(Uri endpoint, string host) => endpoint.Scheme == "http"
        && endpoint.OriginalString == $"http://{host}:4318/v1/metrics"
        && endpoint.Port == 4318
        && endpoint.AbsolutePath == "/v1/metrics"
        && endpoint.UserInfo.Length == 0
        && endpoint.Query.Length == 0
        && endpoint.Fragment.Length == 0;
}
