// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Discord;

/// <summary>Bounds transient conversation history for a particular trigger.</summary>
/// <param name="Messages">The maximum number of preceding messages.</param>
/// <param name="Characters">The maximum combined history text length.</param>
/// <param name="Age">The inclusive lookback duration.</param>
/// <param name="FromSelection">Whether age is measured from the selected message instead of now.</param>
public sealed record ConversationWindow(int Messages, int Characters, TimeSpan Age, bool FromSelection)
{
    /// <summary>The existing recent-channel window.</summary>
    public static readonly ConversationWindow Recent = new(10, 6000, TimeSpan.FromMinutes(15), false);

    /// <summary>The thread window anchored at a selected message.</summary>
    public static readonly ConversationWindow Thread = new(20, 6000, TimeSpan.FromHours(24), true);
}
