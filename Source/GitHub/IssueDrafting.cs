// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Anthropic.Models.Messages;
using Microsoft.Extensions.AI;

namespace Cratis.Prompter.GitHub;

/// <summary>
/// Drafts an issue from a conversation, using the same chat client that answers questions.
/// </summary>
/// <param name="chatClient">The chat client the draft is generated with.</param>
public class IssueDrafting(IChatClient chatClient) : IIssueDrafting
{
    /// <inheritdoc/>
    public async Task<IssueDraft?> Draft(string conversation, string? hint, CancellationToken cancellationToken = default)
    {
        // The system prompt is frozen, so mark it cacheable exactly as answering does (D-5).
        var systemMessage = new ChatMessage(
            ChatRole.System,
            [new TextContent(IssueDraftPrompt.SystemText).WithCacheControl(new CacheControlEphemeral())]);

        var response = await chatClient.GetResponseAsync(
            [systemMessage, new(ChatRole.User, IssueDraftPrompt.UserMessage(conversation, hint))],
            cancellationToken: cancellationToken);

        return IssueDraftParsing.Parse(response.Text);
    }
}
