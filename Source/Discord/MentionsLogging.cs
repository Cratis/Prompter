// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Prompter.Discord;

internal static partial class MentionsLogging
{
    [LoggerMessage(LogLevel.Information, "Answering mention from user {UserId}")]
    internal static partial void AnsweringMention(this ILogger<Mentions> logger, ulong userId);
}
