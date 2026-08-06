// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.GitHub;

/// <summary>
/// Defines drafting an issue from a conversation.
/// </summary>
public interface IIssueDrafting
{
    /// <summary>
    /// Drafts an issue from what someone wrote.
    /// </summary>
    /// <param name="conversation">The conversation text to draft from.</param>
    /// <param name="hint">An optional steer from the reporter.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The draft, or <see langword="null"/> when the model's reply could not be read as one.</returns>
    Task<IssueDraft?> Draft(string conversation, string? hint, CancellationToken cancellationToken = default);
}
