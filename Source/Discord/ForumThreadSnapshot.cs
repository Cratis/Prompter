// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Discord;

/// <summary>
/// The metadata of an active forum post; it carries no post content.
/// </summary>
/// <param name="Id">The thread identifier.</param>
/// <param name="GuildId">The guild identifier.</param>
/// <param name="ParentId">The forum channel identifier.</param>
/// <param name="OwnerId">The identifier of the post's author.</param>
/// <param name="CreatedAt">When the post was created.</param>
/// <param name="Closed">Whether the post is archived or locked.</param>
public sealed record ForumThreadSnapshot(ulong Id, ulong GuildId, ulong ParentId, ulong OwnerId, DateTimeOffset CreatedAt, bool Closed);
