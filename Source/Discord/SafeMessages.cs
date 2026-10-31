// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NetCord;
using NetCord.Rest;

namespace Cratis.Prompter.Discord;

/// <summary>
/// Builds outbound message payloads for text the bot did not author (model output, user text) so that text
/// can never ping <c language="text">@everyone</c>, <c language="text">@here</c>, roles or users.
/// </summary>
public static class SafeMessages
{
    /// <summary>Creates a channel message that mentions nobody.</summary>
    /// <param name="content">The message text.</param>
    /// <returns>The message properties.</returns>
    public static MessageProperties Message(string content) => new() { Content = content, AllowedMentions = AllowedMentionsProperties.None };

    /// <summary>Creates a reply that mentions nobody except the asker the reply references.</summary>
    /// <param name="content">The message text.</param>
    /// <param name="mentionAsker">Whether the referenced author is the requesting member.</param>
    /// <returns>The reply properties.</returns>
    public static ReplyMessageProperties Reply(string content, bool mentionAsker = true) =>
        new() { Content = content, AllowedMentions = mentionAsker ? AllowedMentionsProperties.None.WithReplyMention(true) : AllowedMentionsProperties.None };

    /// <summary>Creates an interaction followup that mentions nobody.</summary>
    /// <param name="content">The message text.</param>
    /// <returns>The interaction message properties.</returns>
    public static InteractionMessageProperties Followup(string content) => new() { Content = content, AllowedMentions = AllowedMentionsProperties.None };

    /// <summary>Creates a private interaction notice without allowing mentions.</summary>
    /// <param name="content">The notice content.</param>
    /// <returns>The ephemeral message properties.</returns>
    public static InteractionMessageProperties Ephemeral(string content)
    {
        var message = Followup(content);
        message.Flags = MessageFlags.Ephemeral;
        return message;
    }
}
