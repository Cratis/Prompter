// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Embeddings;

internal static partial class EmbeddingsLogging
{
    [LoggerMessage(LogLevel.Warning, "Transient embedding failure (status {Status}); retry {Attempt} of {MaxRetries} in {DelayMilliseconds} ms")]
    internal static partial void RetryingAfterTransientFailure(this ILogger<ResilientEmbeddingGenerator> logger, int? status, int attempt, int maxRetries, double delayMilliseconds);
}
