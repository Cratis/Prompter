// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Discord;

internal static partial class MentionsLogging
{
    [LoggerMessage(LogLevel.Information, "Answering {Source} question from user {UserId}")]
    internal static partial void AnsweringQuestion(this ILogger<Mentions> logger, string source, ulong userId);

    [LoggerMessage(LogLevel.Information, "Rate-limited question from user {UserId}")]
    internal static partial void RateLimited(this ILogger<Mentions> logger, ulong userId);

    [LoggerMessage(LogLevel.Error, "Failed to answer question from user {UserId}")]
    internal static partial void AnswerFailed(this ILogger<Mentions> logger, Exception exception, ulong userId);

    [LoggerMessage(LogLevel.Error, "Failed to send apology to user {UserId} after an answer failure")]
    internal static partial void ApologyFailed(this ILogger<Mentions> logger, Exception exception, ulong userId);
}
