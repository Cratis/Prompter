// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Prompter.GitHub;

/// <summary>
/// Authorizes GitHub webhook deliveries by verifying the HMAC signature GitHub sends with the payload.
/// </summary>
/// <remarks>
/// GitHub signs the raw request body with the configured secret and sends the result as
/// <c>X-Hub-Signature-256: sha256=&lt;hex&gt;</c>. Verifying that signature — rather than checking a shared
/// secret in a header — is what proves the delivery came from GitHub and that the body was not altered on the
/// way. An unset secret refuses everything, the same posture the re-index endpoint takes.
/// </remarks>
public static class WebhookAuth
{
    /// <summary>
    /// The header carrying the signature of the payload.
    /// </summary>
    public const string SignatureHeader = "X-Hub-Signature-256";

    /// <summary>
    /// The header naming the event a delivery carries.
    /// </summary>
    public const string EventHeader = "X-GitHub-Event";

    const string Prefix = "sha256=";

    /// <summary>
    /// Determines whether a delivery is authentic.
    /// </summary>
    /// <param name="signature">The value of the signature header, or <see langword="null"/> when absent.</param>
    /// <param name="payload">The exact bytes of the request body, as received.</param>
    /// <param name="secret">The configured webhook secret, or <see langword="null"/>/empty when unset.</param>
    /// <returns>
    /// <see langword="true"/> only when a secret is configured and the signature matches the payload;
    /// otherwise <see langword="false"/>.
    /// </returns>
    public static bool IsAuthentic(string? signature, ReadOnlySpan<byte> payload, string? secret)
    {
        if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(signature))
        {
            return false;
        }

        if (!signature.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var provided = signature[Prefix.Length..];
        var expected = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), payload))
            .ToLower(CultureInfo.InvariantCulture);

        // Both sides are fixed-length hex of a SHA-256 hash, so the constant-time compare runs over
        // equal-length spans and leaks neither length nor how much matched.
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(provided.ToLower(CultureInfo.InvariantCulture)),
            Encoding.UTF8.GetBytes(expected));
    }
}
