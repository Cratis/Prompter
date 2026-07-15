// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Discord;

internal static partial class AskLogging
{
    [LoggerMessage(LogLevel.Information, "Rate-limited /ask from user {UserId}")]
    internal static partial void RateLimited(this ILogger<Ask> logger, ulong userId);

    [LoggerMessage(LogLevel.Error, "Failed to answer /ask from user {UserId}")]
    internal static partial void AnswerFailed(this ILogger<Ask> logger, Exception exception, ulong userId);

    [LoggerMessage(LogLevel.Error, "Failed to send apology to user {UserId} after an /ask failure")]
    internal static partial void ApologyFailed(this ILogger<Ask> logger, Exception exception, ulong userId);
}
