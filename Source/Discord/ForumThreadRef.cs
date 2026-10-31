// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Discord;

/// <summary>
/// Identifies the forum post an answer is for.
/// </summary>
/// <param name="GuildId">The guild identifier.</param>
/// <param name="Id">The thread identifier.</param>
/// <param name="ParentId">The forum channel identifier.</param>
/// <param name="OwnerId">The identifier of the post's author.</param>
public sealed record ForumThreadRef(ulong GuildId, ulong Id, ulong ParentId, ulong OwnerId);
