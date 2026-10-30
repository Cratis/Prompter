// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Discord;

/// <summary>Reads only recent plain text in an admitted request's public channel or thread.</summary>
public interface IConversationHistory
{
    /// <summary>Checks public visibility, failing closed if it cannot be established.</summary>
    /// <param name="location">The exact requested scope.</param>
    /// <param name="cancellationToken">The request's history deadline.</param>
    /// <returns>Whether everyone can view the scope and it is not a private thread.</returns>
    Task<bool> IsPublic(ConversationLocation location, CancellationToken cancellationToken);

    /// <summary>Reads at most ten preceding messages from this scope only; no paging or referenced content.</summary>
    /// <param name="location">The exact requested scope and exclusive upper message id.</param>
    /// <param name="cancellationToken">The request's history deadline.</param>
    /// <returns>Transient plain-text candidates.</returns>
    Task<IReadOnlyList<HistoryMessage>> Before(ConversationLocation location, CancellationToken cancellationToken);
}
