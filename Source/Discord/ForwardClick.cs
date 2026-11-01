// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Discord;

/// <summary>The content-free context of a forward click.</summary>
/// <param name="Kind">What is being forwarded.</param>
/// <param name="Actor">The clicking actor.</param>
/// <param name="CustomId">The clicked action id.</param>
public sealed record ForwardClick(ForwardKind Kind, DiscordActor Actor, string CustomId)
{
    /// <summary>Gets the thread parent from the interaction channel, or null for a non-thread channel.</summary>
    public ulong? ParentChannelId { get; init; }
}
