// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Discord;

/// <summary>A Discord question location, without its content.</summary>
/// <param name="ChannelId">The question channel.</param>
/// <param name="MessageId">The question message, or zero for a slash command.</param>
public sealed record QuestionLink(ulong ChannelId, ulong MessageId)
{
    /// <summary>Gets whether Discord can retrieve the question.</summary>
    public bool HasMessage => MessageId != 0;

    /// <summary>Builds a jump link in a guild.</summary>
    /// <param name="guildId">The guild identifier.</param>
    /// <returns>The Discord jump link.</returns>
    public string JumpLink(ulong guildId) => $"https://discord.com/channels/{guildId}/{ChannelId}/{MessageId}";
}
