// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// Represents the deterministic identifier of a chunk, derived from its page and position.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record ChunkId(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// The value used when a chunk identifier is not set.
    /// </summary>
    public static readonly ChunkId NotSet = new(string.Empty);

    public static implicit operator string(ChunkId id) => id.Value;

    public static implicit operator ChunkId(string value) => new(value);
}
