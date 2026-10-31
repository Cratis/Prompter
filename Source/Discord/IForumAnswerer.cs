// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NetCord.Rest;

namespace Cratis.Prompter.Discord;

/// <summary>
/// Answers a forum post through the shared admission, spend and formatting path.
/// </summary>
public interface IForumAnswerer
{
    /// <summary>Answers a forum post, or refuses or apologizes in-thread when it cannot.</summary>
    /// <param name="thread">The post to answer.</param>
    /// <param name="forumId">The forum the post belongs to.</param>
    /// <param name="included">The starter message when the gateway delivered it, otherwise null.</param>
    /// <param name="delayed">Whether this is a delayed answer rather than the immediate help-forum one.</param>
    /// <returns>A task that completes when the post has been handled.</returns>
    Task Answer(ForumThreadRef thread, ulong forumId, RestMessage? included, bool delayed);
}
