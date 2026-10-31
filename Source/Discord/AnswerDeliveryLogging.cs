// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Discord;

internal static partial class AnswerDeliveryLogging
{
    [LoggerMessage(LogLevel.Warning, "Could not open answer thread in channel {ChannelId}: HTTP {StatusCode}, Discord {ErrorCode}")]
    internal static partial void ThreadUnavailable(this ILogger<AnswerDelivery> logger, ulong channelId, int statusCode, int? errorCode);
}
