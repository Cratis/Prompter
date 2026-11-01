// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Discord;

/// <summary>
/// A validated forum opted into delayed answering.
/// </summary>
/// <param name="ChannelId">The forum channel identifier.</param>
/// <param name="Delay">How long a post waits before it is answered.</param>
public sealed record DelayedForum(ulong ChannelId, TimeSpan Delay);
