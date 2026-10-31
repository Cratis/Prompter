// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using Pulumi;

namespace Cratis.Prompter.Deployment.Services;

/// <summary>Explicit non-secret installation identity and external Secret references only.</summary>
public sealed partial record GitHubAppArgs
{
    /// <summary>Gets the numeric App ID.</summary>
    public string AppId { get; init; } = string.Empty;

    /// <summary>Gets the numeric installation ID.</summary>
    public string InstallationId { get; init; } = string.Empty;

    /// <summary>Gets the single approved owner.</summary>
    public string Owner { get; init; } = string.Empty;

    /// <summary>Gets the exact comma-separated owner/repository allowlist.</summary>
    public string Repositories { get; init; } = string.Empty;

    /// <summary>Gets the existing namespace-local Secret name, never its content.</summary>
    public string ExternalSecretName { get; init; } = string.Empty;

    /// <summary>Gets the single key to project from the external Secret.</summary>
    public string KeyReference { get; init; } = string.Empty;

    /// <summary>Gets whether any App setting is supplied.</summary>
    public bool IsConfigured => new[] { AppId, InstallationId, Owner, Repositories, ExternalSecretName, KeyReference }.Any(value => !string.IsNullOrEmpty(value));

    /// <summary>Gets whether all configured inputs form a complete explicit identity.</summary>
    public bool IsValid => !IsConfigured || (Id(AppId) && Id(InstallationId) && Identifier(Owner)
        && Repositories.Split(',') is var repositories && repositories.Length > 0
        && repositories.All(Repository) && repositories.Distinct(StringComparer.OrdinalIgnoreCase).Count() == repositories.Length
        && ExternalSecretName.Length <= 253 && ExternalSecretName.Split('.').All(DnsLabel.IsMatch)
        && KeyReference.Length is > 0 and <= 253 && KeyReference is not "." and not ".."
        && KeyReference.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.'));

    [GeneratedRegex(@"\A[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\z", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex DnsLabel { get; }

    /// <summary>Reads only non-secret deployment inputs.</summary>
    /// <param name="config">Deployment configuration.</param>
    /// <returns>The explicit identity.</returns>
    public static GitHubAppArgs From(Config config) => new()
    {
        AppId = config.Get("gitHubAppId") ?? string.Empty,
        InstallationId = config.Get("gitHubInstallationId") ?? string.Empty,
        Owner = config.Get("gitHubOwner") ?? string.Empty,
        Repositories = config.Get("gitHubInstallationRepositories") ?? string.Empty,
        ExternalSecretName = config.Get("gitHubAppExternalSecretName") ?? string.Empty,
        KeyReference = config.Get("gitHubAppKeyReference") ?? string.Empty
    };

    static bool Id(string value) => long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0;
    static bool Identifier(string value) => !string.IsNullOrEmpty(value) && value.Length <= 100 && value is not "." and not ".."
        && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.');
    bool Repository(string value) => value.Split('/') is [var owner, var name] && owner.Equals(Owner, StringComparison.OrdinalIgnoreCase) && Identifier(name);
}
