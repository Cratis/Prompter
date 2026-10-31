// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Discord;

/// <summary>Judges whether a selected message can be answered without fetching or paid work.</summary>
public static class MessageSelection
{
    /// <summary>Rejects system, bot, empty, and oversized selections in that order.</summary>
    /// <param name="content">The selected plain text.</param>
    /// <param name="isBot">Whether any bot, including Prompter, authored it.</param>
    /// <param name="isSystem">Whether Discord generated the message.</param>
    /// <param name="maximumCharacters">The selection's character ceiling.</param>
    /// <returns>The selection verdict.</returns>
    public static MessageSelectionVerdict For(string content, bool isBot, bool isSystem, int maximumCharacters)
    {
        if (isSystem) return MessageSelectionVerdict.System;
        if (isBot) return MessageSelectionVerdict.FromBot;
        if (string.IsNullOrWhiteSpace(content)) return MessageSelectionVerdict.Empty;
        return content.Length > maximumCharacters ? MessageSelectionVerdict.TooLong : MessageSelectionVerdict.Accept;
    }
}
