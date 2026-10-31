// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Discord;

/// <summary>
/// The pure rules deciding which forums are delayed, which posts are due, and whether a post still needs an answer.
/// </summary>
public static class DelayedForumSchedule
{
    /// <summary>The largest configurable delay in minutes.</summary>
    public const int MaximumDelayMinutes = 1440;

    /// <summary>How many recent messages are read to judge a post; a thread this busy is left alone.</summary>
    public const int ReplyScanLimit = 50;

    /// <summary>How long after its delay a post is still answered, so enabling the feature never answers an old backlog.</summary>
    public static readonly TimeSpan MaximumLateness = TimeSpan.FromHours(24);

    /// <summary>
    /// Reads the usable delayed forums. Invalid entries, the immediate help forum and repeats are dropped.
    /// </summary>
    /// <param name="discord">The Discord options.</param>
    /// <returns>The validated delayed forums.</returns>
    public static IReadOnlyList<DelayedForum> Configured(DiscordOptions discord)
    {
        var forums = new List<DelayedForum>();
        foreach (var entry in discord.DelayedForums)
        {
            if (!IsUsable(entry, discord) || forums.Exists(forum => forum.ChannelId == entry.ChannelId))
            {
                continue;
            }

            forums.Add(new(entry.ChannelId, TimeSpan.FromMinutes(entry.DelayMinutes)));
        }

        return forums;
    }

    /// <summary>
    /// Decides whether a post has waited its delay and is still inside the lateness window.
    /// </summary>
    /// <param name="thread">The post.</param>
    /// <param name="delay">The forum's delay.</param>
    /// <param name="now">The current time.</param>
    /// <returns>Whether the post is due.</returns>
    public static bool IsDue(ForumThreadSnapshot thread, TimeSpan delay, DateTimeOffset now)
    {
        var age = now - thread.CreatedAt;
        return !thread.Closed && age >= delay && age <= delay + MaximumLateness;
    }

    /// <summary>
    /// Reads whether a post still needs an answer from its newest messages.
    /// </summary>
    /// <param name="recent">The newest messages.</param>
    /// <param name="threadId">The thread identifier; the message with this identifier is the starter.</param>
    /// <param name="ownerId">The post author's identifier.</param>
    /// <param name="botId">Prompter's own user identifier.</param>
    /// <returns>The reply state.</returns>
    public static ForumReplyState Replies(IReadOnlyList<ForumMessage> recent, ulong threadId, ulong ownerId, ulong botId)
    {
        var others = recent.Where(message => message.Id != threadId).ToList();
        if (others.Exists(message => message.AuthorId == botId))
        {
            return ForumReplyState.PrompterAnswered;
        }

        if (others.Exists(message => IsHumanReply(message, ownerId)))
        {
            return ForumReplyState.HumanReplied;
        }

        return recent.Count >= ReplyScanLimit ? ForumReplyState.Busy : ForumReplyState.Unanswered;
    }

    static bool IsUsable(DelayedForumOptions entry, DiscordOptions discord) =>
        entry.ChannelId != 0 && entry.DelayMinutes is >= 1 and <= MaximumDelayMinutes && entry.ChannelId != discord.HelpForumChannelId;

    static bool IsHumanReply(ForumMessage message, ulong ownerId) =>
        !message.AuthorIsBot && !message.IsWebhook && message.IsUserContent && message.AuthorId != ownerId;
}
