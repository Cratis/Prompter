// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Discord;

internal static partial class MentionsLogging
{
    [LoggerMessage(LogLevel.Information, "Answering {Source} question")]
    internal static partial void AnsweringQuestion(this ILogger<Mentions> logger, string source);

    [LoggerMessage(LogLevel.Information, "Rate-limited question")]
    internal static partial void RateLimited(this ILogger<Mentions> logger);

    [LoggerMessage(LogLevel.Error, "Failed to answer question")]
    internal static partial void AnswerFailed(this ILogger<Mentions> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Failed to send apology after an answer failure")]
    internal static partial void ApologyFailed(this ILogger<Mentions> logger, Exception exception);
}
