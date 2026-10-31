// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Discord;

internal static partial class HumanHandOffsLogging
{
    [LoggerMessage(LogLevel.Warning, "Human hand-off question unavailable: HTTP {StatusCode}, Discord {ErrorCode}")]
    internal static partial void QuestionUnavailable(this ILogger<HumanHandOffs> logger, int statusCode, int? errorCode);

    [LoggerMessage(LogLevel.Warning, "Human hand-off send unavailable: HTTP {StatusCode}, Discord {ErrorCode}")]
    internal static partial void SendUnavailable(this ILogger<HumanHandOffs> logger, int statusCode, int? errorCode);
}
