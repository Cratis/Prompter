// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Answering;

internal static partial class AnswersLogging
{
    [LoggerMessage(LogLevel.Information, "Refusing to answer - no passage meets the semantic similarity threshold; highest similarity {Confidence}")]
    internal static partial void RefusingToAnswer(this ILogger<Answers> logger, double confidence);

    [LoggerMessage(LogLevel.Information, "Refusing to answer - the question is out of scope; top qualified similarity {Similarity}")]
    internal static partial void RefusingOutOfScope(this ILogger<Answers> logger, double similarity);

    [LoggerMessage(LogLevel.Information, "Answering with top qualified passage {PageUrl}; semantic similarity {Similarity}")]
    internal static partial void AnsweringWithPassage(this ILogger<Answers> logger, string pageUrl, double similarity);

    [LoggerMessage(LogLevel.Warning, "Could not parse answer generation; response length {ResponseLength}, finish reason {FinishReason}")]
    internal static partial void GenerationParseFailed(this ILogger<Answers> logger, int responseLength, string? finishReason);

    [LoggerMessage(LogLevel.Warning, "Answer generation reached the output limit of {MaxOutputTokens} tokens; output tokens {OutputTokens}, salvaged {Salvaged}")]
    internal static partial void AnswerReachedOutputLimit(this ILogger<Answers> logger, int maxOutputTokens, long? outputTokens, bool salvaged);

    [LoggerMessage(LogLevel.Warning, "Removed retrieval meta-talk from a generated answer")]
    internal static partial void RemovedRetrievalMetaTalk(this ILogger<Answers> logger);
}
