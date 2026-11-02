// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Discord;

internal static partial class FeedbackLogging
{
    [LoggerMessage(LogLevel.Information, "Recorded feedback")]
    internal static partial void RecordedFeedback(this ILogger<FeedbackProcessing> logger);

    [LoggerMessage(LogLevel.Warning, "Ignoring feedback click with unrecognized custom id")]
    internal static partial void UnrecognizedFeedbackCustomId(this ILogger<FeedbackProcessing> logger);

    [LoggerMessage(LogLevel.Warning, "Failed to record feedback")]
    internal static partial void FeedbackWriteFailed(this ILogger<FeedbackProcessing> logger);

    [LoggerMessage(LogLevel.Debug, "Ignoring repeated feedback click")]
    internal static partial void FeedbackRepeated(this ILogger<FeedbackProcessing> logger);

    [LoggerMessage(LogLevel.Information, "Feedback processing canceled")]
    internal static partial void FeedbackProcessingCanceled(this ILogger<FeedbackProcessing> logger);

    [LoggerMessage(LogLevel.Information, "Refused a feedback click over its budget or memory bound")]
    internal static partial void FeedbackClickRefused(this ILogger<Feedback> logger);
}
