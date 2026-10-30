// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Discord;

/// <summary>Identifies only the triggering public channel or thread and its history cutoff.</summary>
/// <param name="GuildId">The guild containing the conversation.</param>
/// <param name="ChannelId">The channel or thread, never a parent or sibling.</param>
/// <param name="MessageId">The exclusive upper message id.</param>
/// <param name="CreatedAt">The triggering message's creation time.</param>
public sealed record ConversationLocation(ulong GuildId, ulong ChannelId, ulong MessageId, DateTimeOffset CreatedAt);
