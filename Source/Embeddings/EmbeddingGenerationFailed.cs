// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Embeddings;

/// <summary>
/// The exception that is thrown when the embedding API returns an unusable response.
/// </summary>
/// <param name="reason">The reason the generation failed.</param>
public class EmbeddingGenerationFailed(string reason)
    : Exception($"Embedding generation failed: {reason}");
