// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;

namespace Cratis.Prompter.Deployment.Storage;

/// <summary>
/// Public configuration only: credentials and the private age identity are externally managed Secrets.
/// </summary>
public sealed record PostgresBackupsArgs
{
    static readonly IPNetwork[] _nonPublicNetworks =
    [
        IPNetwork.Parse("0.0.0.0/8"), IPNetwork.Parse("10.0.0.0/8"), IPNetwork.Parse("100.64.0.0/10"),
        IPNetwork.Parse("127.0.0.0/8"), IPNetwork.Parse("169.254.0.0/16"), IPNetwork.Parse("172.16.0.0/12"),
        IPNetwork.Parse("192.168.0.0/16"), IPNetwork.Parse("224.0.0.0/3"),
    ];

    /// <summary>Gets the existing application namespace.</summary>
    public required string Namespace { get; init; }

    /// <summary>Gets the environment label.</summary>
    public required string Environment { get; init; }

    /// <summary>Gets the approved PG17/pgvector tooling image, pinned by SHA256 digest.</summary>
    public required string Image { get; init; }

    /// <summary>Gets the public HTTPS object-store endpoint.</summary>
    public required string Endpoint { get; init; }

    /// <summary>Gets the approved public object-store IPv4 /32 CIDR.</summary>
    public required string StoreCidr { get; init; }

    /// <summary>Gets the dedicated unversioned bucket with a verified 14-day lifecycle.</summary>
    public required string Bucket { get; init; }

    /// <summary>Gets the public age recipient, never the private identity.</summary>
    public required string Recipient { get; init; }

    /// <summary>Rejects configuration that is not pinned to approved tooling and object storage.</summary>
    /// <exception cref="InvalidPostgresBackupConfiguration">A public configuration value is unsafe.</exception>
    internal void Validate()
    {
        var requirements = new[] { IsPinnedImage(), IsHttpsEndpoint(), IsPublicStore(), !string.IsNullOrWhiteSpace(Bucket), Recipient.StartsWith("age1", StringComparison.Ordinal) };
        if (requirements.Contains(false))
        {
            throw new InvalidPostgresBackupConfiguration();
        }
    }

    bool IsPinnedImage()
    {
        var digest = Image.Split("@sha256:", StringSplitOptions.None);
        return digest.Length == 2 && digest[0].Length > 0 && digest[1].Length == 64 && digest[1].All(char.IsAsciiHexDigit);
    }

    bool IsHttpsEndpoint() =>
        Uri.TryCreate(Endpoint, UriKind.Absolute, out var endpoint) && endpoint.Scheme == "https" &&
        endpoint.UserInfo.Length == 0 && endpoint.Port == 443 && endpoint.Query.Length == 0 && endpoint.Fragment.Length == 0;

    bool IsPublicStore()
    {
        if (!StoreCidr.EndsWith("/32", StringComparison.Ordinal))
        {
            return false;
        }

        if (!IPAddress.TryParse(StoreCidr[..^3], out var address))
        {
            return false;
        }

        return address.AddressFamily == AddressFamily.InterNetwork && !_nonPublicNetworks.Any(network => network.Contains(address));
    }
}
