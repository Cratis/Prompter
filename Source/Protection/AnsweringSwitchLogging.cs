// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Protection;

internal static partial class AnsweringSwitchLogging
{
    [LoggerMessage(LogLevel.Warning, "Answering paused")]
    internal static partial void AnsweringPaused(this ILogger logger);

    [LoggerMessage(LogLevel.Information, "Answering resumed")]
    internal static partial void AnsweringResumed(this ILogger logger);
}
