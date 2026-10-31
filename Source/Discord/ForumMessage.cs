// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Discord;

/// <summary>
/// The author facts of a message in a forum post; it carries no text.
/// </summary>
/// <param name="Id">The message identifier.</param>
/// <param name="AuthorId">The author identifier.</param>
/// <param name="AuthorIsBot">Whether the author is a bot.</param>
/// <param name="IsWebhook">Whether a webhook posted the message.</param>
/// <param name="IsUserContent">Whether the message is ordinary user content rather than a system message.</param>
public sealed record ForumMessage(ulong Id, ulong AuthorId, bool AuthorIsBot, bool IsWebhook, bool IsUserContent);
