// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Answering;

internal static partial class AnswersLogging
{
    [LoggerMessage(LogLevel.Information, "Refusing to answer '{Question}' - top passage score {Confidence} is below the threshold")]
    internal static partial void RefusingToAnswer(this ILogger<Answers> logger, Question question, double confidence);
}
