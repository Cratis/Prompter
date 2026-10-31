// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Discord;

internal static partial class GuidanceButtonsLogging
{
    [LoggerMessage(LogLevel.Information, "Rate-limited a guidance button")]
    internal static partial void RateLimited(this ILogger<GuidanceButtons> logger);

    [LoggerMessage(LogLevel.Error, "Failed to answer a guidance button")]
    internal static partial void AnswerFailed(this ILogger<GuidanceButtons> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Failed to send apology after a guidance button failure")]
    internal static partial void ApologyFailed(this ILogger<GuidanceButtons> logger, Exception exception);
}
