// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Answering;

internal static partial class AnswersLogging
{
    [LoggerMessage(LogLevel.Information, "Refusing to answer - no passage meets the semantic similarity threshold; highest similarity {Confidence}")]
    internal static partial void RefusingToAnswer(this ILogger<Answers> logger, double confidence);

    [LoggerMessage(LogLevel.Information, "Answering with top qualified passage {PageUrl}; semantic similarity {Similarity}")]
    internal static partial void AnsweringWithPassage(this ILogger<Answers> logger, string pageUrl, double similarity);

    [LoggerMessage(LogLevel.Warning, "Could not parse answer generation; response length {ResponseLength}, finish reason {FinishReason}")]
    internal static partial void GenerationParseFailed(this ILogger<Answers> logger, int responseLength, string? finishReason);
}
