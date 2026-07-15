// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// Represents the content hash of a chunk, used to detect changed content between index runs.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record ContentHash(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The value used when a content hash is not set.
    /// </summary>
    public static readonly ContentHash NotSet = new(string.Empty);

    public static implicit operator string(ContentHash hash) => hash.Value;

    public static implicit operator ContentHash(string value) => new(value);

    /// <summary>
    /// Computes the hash for the given content.
    /// </summary>
    /// <param name="content">The content to hash.</param>
    /// <returns>The computed <see cref="ContentHash"/>.</returns>
    public static ContentHash For(string content) =>
        new(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content))));
}
