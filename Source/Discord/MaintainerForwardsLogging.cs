// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Discord;

internal static partial class MaintainerForwardsLogging
{
    [LoggerMessage(LogLevel.Warning, "Forward question unavailable: HTTP {StatusCode}, Discord {ErrorCode}")]
    internal static partial void QuestionUnavailable(this ILogger<MaintainerForwards> logger, int statusCode, int? errorCode);

    [LoggerMessage(LogLevel.Warning, "Forward to the maintainer channel failed")]
    internal static partial void PostFailed(this ILogger<MaintainerForwards> logger, Exception exception);
}
