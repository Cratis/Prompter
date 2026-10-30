// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;

namespace Cratis.Prompter.Routing;

/// <summary>Fail-closed origin validation without DNS or network probes.</summary>
internal static class PrivateDecisionEndpoint
{
    /// <summary>Checks explicit host approval and private addressing conventions.</summary>
    /// <param name="options">Operator-owned configuration.</param>
    /// <returns>Whether the origin may receive classification requests.</returns>
    public static bool IsAllowed(RoutingOptions options)
    {
        var endpoint = options.Endpoint;
        if (endpoint is not { IsAbsoluteUri: true }) return false;
        return IsServiceOrigin(endpoint)
            && string.Equals(endpoint.DnsSafeHost, options.AllowedHost, StringComparison.OrdinalIgnoreCase)
            && IsPrivateHost(endpoint.DnsSafeHost);
    }

    static bool IsServiceOrigin(Uri endpoint) => (endpoint.Scheme == "http" || endpoint.Scheme == "https")
        && endpoint.AbsolutePath == "/" && endpoint.Query.Length == 0 && endpoint.Fragment.Length == 0 && endpoint.UserInfo.Length == 0;

    static bool IsPrivateHost(string host)
    {
        if (IPAddress.TryParse(host, out var address)) return IsPrivateAddress(address);
        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".svc", StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(".cluster.local", StringComparison.OrdinalIgnoreCase);
    }

    static bool IsPrivateAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (IPAddress.IsLoopback(address)) return true;
        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == AddressFamily.InterNetworkV6) return (bytes[0] & 0xfe) == 0xfc;
        return bytes[0] == 10 || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) || (bytes[0] == 192 && bytes[1] == 168);
    }
}
