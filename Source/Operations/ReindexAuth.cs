// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Prompter.Operations;

/// <summary>
/// Authorizes re-index requests by comparing a caller-supplied shared secret with the configured one.
/// </summary>
public static class ReindexAuth
{
    /// <summary>
    /// Determines whether a re-index request carrying <paramref name="providedSecret"/> is authorized against
    /// the <paramref name="configuredSecret"/>.
    /// </summary>
    /// <param name="providedSecret">The secret presented by the caller, or <see langword="null"/> when absent.</param>
    /// <param name="configuredSecret">The configured shared secret, or <see langword="null"/>/empty when unset.</param>
    /// <returns>
    /// <see langword="true"/> only when a non-empty secret is configured and the presented secret matches it;
    /// otherwise <see langword="false"/>. An unset configured secret refuses every caller.
    /// </returns>
    /// <remarks>
    /// Both secrets are SHA-256 hashed to a fixed 32-byte length before being compared with
    /// <see cref="CryptographicOperations.FixedTimeEquals(ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>. Comparing
    /// the raw UTF-8 bytes would let <c>FixedTimeEquals</c> short-circuit on a length mismatch, leaking the
    /// configured secret's length through timing; equal-length hashes run in constant time and leak neither the
    /// length nor how much of the secret matched.
    /// </remarks>
    public static bool IsAuthorized(string? providedSecret, string? configuredSecret)
    {
        if (string.IsNullOrEmpty(configuredSecret) || string.IsNullOrEmpty(providedSecret))
        {
            return false;
        }

        var provided = SHA256.HashData(Encoding.UTF8.GetBytes(providedSecret));
        var configured = SHA256.HashData(Encoding.UTF8.GetBytes(configuredSecret));

        return CryptographicOperations.FixedTimeEquals(provided, configured);
    }
}
