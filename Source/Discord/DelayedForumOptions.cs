// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Discord;

/// <summary>
/// Opts one forum channel into delayed answering of posts nobody has answered.
/// </summary>
public class DelayedForumOptions
{
    /// <summary>Gets or sets the forum channel identifier. Zero is ignored.</summary>
    public ulong ChannelId { get; set; }

    /// <summary>Gets or sets how many minutes a post waits for a human before Prompter answers (1 to 1440).</summary>
    public int DelayMinutes { get; set; } = 30;
}
