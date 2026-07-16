// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Prompter.Discord;

/// <summary>
/// Hashes Discord user identifiers one-way before they touch the interaction log,
/// per the GDPR decision record (D-8).
/// </summary>
/// <remarks>
/// The hash is keyed (HMAC-SHA256), not a bare digest. A Discord snowflake is public and enumerable, and this
/// code is public, so an unkeyed <c>SHA256(userId)</c> would be trivially reversible by anyone holding the
/// interaction log: they could hash the server's member roster and match every row. Keying with a secret the
/// log holder does not have is what makes the stored value an unlinkable pseudonym, which is the protection
/// D-8 promises. The key is required whenever a Discord token is configured (validated at startup).
/// </remarks>
public static class UserHash
{
    /// <summary>
    /// Computes the keyed hash for a Discord user identifier.
    /// </summary>
    /// <param name="userId">The Discord snowflake identifier of the user.</param>
    /// <param name="key">The secret key that makes the hash unlinkable without it.</param>
    /// <returns>The hash as a hex string.</returns>
    public static string For(ulong userId, string key) =>
        Convert.ToHexStringLower(
            HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(key),
                Encoding.UTF8.GetBytes($"prompter:{userId}")));
}
