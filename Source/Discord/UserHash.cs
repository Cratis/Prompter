// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Prompter.Discord;

/// <summary>
/// Hashes Discord user identifiers one-way before they touch the interaction log,
/// per the GDPR decision record (D-8).
/// </summary>
public static class UserHash
{
    /// <summary>
    /// Computes the hash for a Discord user identifier.
    /// </summary>
    /// <param name="userId">The Discord snowflake identifier of the user.</param>
    /// <returns>The hash as a hex string.</returns>
    public static string For(ulong userId) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"prompter:{userId}")));
}
