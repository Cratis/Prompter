// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.GitHub;

/// <summary>
/// A drafted issue waiting for its reporter to confirm it.
/// </summary>
/// <param name="Draft">The drafted issue.</param>
/// <param name="Repository">The repository it will be filed in.</param>
/// <param name="ConversationUrl">A link back to the conversation it came from, if available.</param>
public record PendingIssue(IssueDraft Draft, string Repository, string? ConversationUrl);
