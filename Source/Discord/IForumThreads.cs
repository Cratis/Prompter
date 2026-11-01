// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Discord;

/// <summary>
/// Reads forum post metadata from Discord.
/// </summary>
public interface IForumThreads
{
    /// <summary>Lists the active posts of a forum.</summary>
    /// <param name="forumId">The forum channel identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The active posts whose parent is the forum.</returns>
    Task<IReadOnlyList<ForumThreadSnapshot>> Active(ulong forumId, CancellationToken cancellationToken);

    /// <summary>Reads the newest messages of a post.</summary>
    /// <param name="threadId">The thread identifier.</param>
    /// <param name="limit">The maximum number of messages.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>At most <paramref name="limit"/> of the newest messages.</returns>
    Task<IReadOnlyList<ForumMessage>> Recent(ulong threadId, int limit, CancellationToken cancellationToken);

    /// <summary>Gets Prompter's own user identifier.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The bot user identifier.</returns>
    Task<ulong> BotId(CancellationToken cancellationToken);
}
