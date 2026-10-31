// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Discord;

/// <summary>Describes the location and author relationship for an answer.</summary>
/// <param name="GuildId">The guild, if any.</param>
/// <param name="InThread">Whether the question is already in a thread.</param>
/// <param name="Question">The transient question used to name a new thread.</param>
/// <param name="OwnMessage">Whether the requester authored the referenced message.</param>
public record AnswerContext(ulong? GuildId, bool InThread, string Question, bool OwnMessage = true);
