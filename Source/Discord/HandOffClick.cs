// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Discord;

/// <summary>The content-free context of an explicit hand-off click.</summary>
/// <param name="Actor">The clicking actor.</param>
/// <param name="CustomId">The action id.</param>
/// <param name="Conversation">The conversation cooldown key.</param>
/// <param name="ClickedMessageId">The bot answer message.</param>
/// <param name="CommandAsker">The original slash-command user, if available.</param>
public sealed record HandOffClick(DiscordActor Actor, string CustomId, ulong Conversation, ulong ClickedMessageId, ulong? CommandAsker);
