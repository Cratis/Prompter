// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Discord;

internal static partial class FeedbackLogging
{
    [LoggerMessage(LogLevel.Information, "Recorded {Verdict} feedback on interaction {InteractionId}")]
    internal static partial void RecordedFeedback(this ILogger<Feedback> logger, string verdict, long interactionId);

    [LoggerMessage(LogLevel.Warning, "Ignoring feedback click with unrecognized custom id {CustomId}")]
    internal static partial void UnrecognizedFeedbackCustomId(this ILogger<Feedback> logger, string customId);

    [LoggerMessage(LogLevel.Warning, "Failed to record {Verdict} feedback on interaction {InteractionId}")]
    internal static partial void FeedbackWriteFailed(this ILogger<Feedback> logger, Exception exception, string verdict, long interactionId);
}
