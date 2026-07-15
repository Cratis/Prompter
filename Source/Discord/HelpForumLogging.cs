// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Discord;

internal static partial class HelpForumLogging
{
    [LoggerMessage(LogLevel.Information, "Answering help-forum thread {ThreadId} started by user {UserId}")]
    internal static partial void AnsweringForumThread(this ILogger<HelpForum> logger, ulong threadId, ulong userId);

    [LoggerMessage(LogLevel.Information, "Rate-limited help-forum thread {ThreadId} started by user {UserId}")]
    internal static partial void RateLimited(this ILogger<HelpForum> logger, ulong threadId, ulong userId);

    [LoggerMessage(LogLevel.Error, "Failed to answer help-forum thread {ThreadId}")]
    internal static partial void AnswerFailed(this ILogger<HelpForum> logger, Exception exception, ulong threadId);

    [LoggerMessage(LogLevel.Error, "Failed to post apology in help-forum thread {ThreadId} after an answer failure")]
    internal static partial void ApologyFailed(this ILogger<HelpForum> logger, Exception exception, ulong threadId);
}
