// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter;

/// <summary>
/// Options for the Discord integration.
/// </summary>
public class DiscordOptions
{
    /// <summary>
    /// Gets or sets the bot token from the Discord developer portal.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the help forum channel that gets automatic replies to new threads.
    /// </summary>
    public ulong? HelpForumChannelId { get; set; }
}
