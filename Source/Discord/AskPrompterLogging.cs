// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Discord;

internal static partial class AskPrompterLogging
{
    [LoggerMessage(LogLevel.Error, "Failed to answer Ask Prompter message command")]
    internal static partial void AnswerFailed(this ILogger<AskPrompter> logger, Exception exception);

    [LoggerMessage(LogLevel.Error, "Failed to send apology after an Ask Prompter message command failure")]
    internal static partial void ApologyFailed(this ILogger<AskPrompter> logger, Exception exception);
}
