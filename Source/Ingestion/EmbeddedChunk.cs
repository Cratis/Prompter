// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Ingestion;

/// <summary>
/// A staged chunk and vector, not visible to retrieval until the complete run commits.
/// </summary>
/// <param name="Chunk">The changed chunk.</param>
/// <param name="Vector">The generated embedding.</param>
public record EmbeddedChunk(Chunk Chunk, ReadOnlyMemory<float> Vector);
