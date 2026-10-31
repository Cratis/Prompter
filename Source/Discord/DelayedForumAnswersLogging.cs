// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Discord;

internal static partial class DelayedForumAnswersLogging
{
    [LoggerMessage(LogLevel.Information, "Delayed forum answering is disabled: no delayed forum is configured")]
    internal static partial void Disabled(this ILogger<DelayedForumAnswers> logger);

    [LoggerMessage(LogLevel.Information, "Answering unanswered forum thread {ThreadId} after its delay")]
    internal static partial void AnsweringDelayedThread(this ILogger<DelayedForumAnswers> logger, ulong threadId);

    [LoggerMessage(LogLevel.Warning, "Sweeping delayed forum {ForumId} failed")]
    internal static partial void SweepFailed(this ILogger<DelayedForumAnswers> logger, ulong forumId);

    [LoggerMessage(LogLevel.Warning, "A delayed forum sweep failed")]
    internal static partial void SweepsFailed(this ILogger<DelayedForumAnswers> logger);
}
