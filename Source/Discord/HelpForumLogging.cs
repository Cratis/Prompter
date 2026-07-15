// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Discord;

internal static partial class HelpForumLogging
{
    [LoggerMessage(LogLevel.Information, "Answering help-forum thread {ThreadId} started by user {UserId}")]
    internal static partial void AnsweringForumThread(this ILogger<HelpForum> logger, ulong threadId, ulong userId);
}
