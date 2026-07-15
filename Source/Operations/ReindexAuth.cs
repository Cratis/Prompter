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
    /// The comparison uses <see cref="CryptographicOperations.FixedTimeEquals(ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>
    /// over the UTF-8 bytes so it runs in constant time and never leaks how much of the secret matched.
    /// </remarks>
    public static bool IsAuthorized(string? providedSecret, string? configuredSecret)
    {
        if (string.IsNullOrEmpty(configuredSecret) || string.IsNullOrEmpty(providedSecret))
        {
            return false;
        }

        var provided = Encoding.UTF8.GetBytes(providedSecret);
        var configured = Encoding.UTF8.GetBytes(configuredSecret);

        return CryptographicOperations.FixedTimeEquals(provided, configured);
    }
}
