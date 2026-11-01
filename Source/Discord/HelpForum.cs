// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace Cratis.Prompter.Discord;

/// <summary>
/// Answers new threads created in the configured help forum, posting a grounded answer as the first
/// reply followed by a standing note that a human will follow up. A delivered answer also lets the thread's author
/// keep asking without a mention (answered by <see cref="Mentions"/>) until another human joins in.
/// </summary>
/// <param name="answerer">The shared forum answer path.</param>
/// <param name="admission">The cheap shared admission guard.</param>
/// <param name="options">The Prompter options carrying the help-forum channel identifier.</param>
public class HelpForum(
    IForumAnswerer answerer,
    Admission admission,
    IOptions<PrompterOptions> options) : IGuildThreadCreateGatewayHandler
{
    /// <summary>
    /// The standing note posted after every auto-reply, inviting a human follow-up and pointing at the
    /// feedback buttons on the answer.
    /// </summary>
    public const string FollowUpNote = "A human will follow up — the 👍/👎 buttons above tell us if this helped.";

    /// <summary>
    /// Decides whether a newly created thread should receive an automatic answer.
    /// </summary>
    /// <param name="parentChannelId">The identifier of the thread's parent channel, if any.</param>
    /// <param name="helpForumChannelId">
    /// The configured help-forum channel identifier, or <see langword="null"/> when the feature is unset.
    /// </param>
    /// <param name="authorIsBot">Whether the thread's starter message was written by a bot.</param>
    /// <param name="starterText">The text of the thread's starter message.</param>
    /// <returns>
    /// <see langword="true"/> when the thread lives in the configured help forum, was started by a human,
    /// and carries a non-empty question; otherwise <see langword="false"/>.
    /// </returns>
    public static bool ShouldAnswer(ulong? parentChannelId, ulong? helpForumChannelId, bool authorIsBot, string? starterText) =>
        helpForumChannelId is { } forumId &&
        parentChannelId == forumId &&
        !authorIsBot &&
        !string.IsNullOrWhiteSpace(starterText);

    /// <inheritdoc/>
    /// <remarks>
    /// The shared <see cref="IForumAnswerer"/> guards the whole answer: the starter's author is throttled before
    /// the expensive answer path, answering runs under a timeout, and any failure is logged and answered
    /// in-thread with a short apology instead of leaving the thread silent.
    /// </remarks>
    public async ValueTask HandleAsync(GuildThreadCreateEventArgs arg)
    {
        var thread = arg.Thread;
        if (!SupportedEvent(arg))
        {
            return;
        }

        await answerer.Answer(
            new(thread.GuildId, thread.Id, thread.ParentId, thread.OwnerId),
            options.Value.Discord.HelpForumChannelId!.Value,
            (thread as ForumGuildThread)?.Message,
            delayed: false);
    }

    bool SupportedEvent(GuildThreadCreateEventArgs arg) => arg.NewlyCreated && SupportedThread(arg.Thread);

    bool SupportedThread(GuildThread thread) =>
        options.Value.Discord.HelpForumChannelId is { } forum && thread.ParentId == forum &&
        admission.InScope(thread.GuildId, forum) && admission.InScope(thread.GuildId, thread.Id);
}
