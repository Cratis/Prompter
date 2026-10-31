// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Protection;

/// <summary>Transient Discord envelope metadata. Never persisted by protection.</summary>
/// <param name="GuildId">The source guild, absent for DMs.</param>
/// <param name="ChannelId">The source channel/thread.</param>
/// <param name="UserId">The author.</param>
public sealed record DiscordActor(ulong? GuildId, ulong ChannelId, ulong UserId);
