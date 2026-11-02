// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Discord;

internal static partial class AnsweringSwitchRefreshLogging
{
    [LoggerMessage(LogLevel.Warning, "Could not read the answering pause flag; keeping the last value")]
    internal static partial void RefreshFailed(this ILogger<AnsweringSwitchRefresh> logger, Exception exception);
}
