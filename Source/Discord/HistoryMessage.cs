// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Discord;

/// <summary>A transient history candidate. Transport metadata is discarded before model input.</summary>
/// <param name="Id">The message id, used only for ordering and exclusion.</param>
/// <param name="ChannelId">The channel id, used only to enforce scope.</param>
/// <param name="CreatedAt">The timestamp, used only for the age limit.</param>
/// <param name="Content">Plain message content, without attachments or embeds.</param>
/// <param name="IsBot">Whether the author is a bot.</param>
/// <param name="IsSelf">Whether this bot authored the message.</param>
/// <param name="IsSystem">Whether this is a system message.</param>
public sealed record HistoryMessage(ulong Id, ulong ChannelId, DateTimeOffset CreatedAt, string Content, bool IsBot, bool IsSelf, bool IsSystem);
