// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Prompter.Answering;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Rest;

namespace Cratis.Prompter.Discord;

/// <summary>
/// Answers new threads created in the configured help forum, posting a grounded answer as the first
/// reply followed by a standing note that a human will follow up.
/// </summary>
/// <param name="rest">The REST client used to read the starter message and post replies into the thread.</param>
/// <param name="answers">The answers Prompter can give.</param>
/// <param name="interactionLog">The interaction log, used to record which message the answer landed on.</param>
/// <param name="options">The Prompter options carrying the help-forum channel identifier.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class HelpForum(
    RestClient rest,
    IAnswers answers,
    IInteractionLog interactionLog,
    IOptions<PrompterOptions> options,
    ILogger<HelpForum> logger) : IGuildThreadCreateGatewayHandler
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
    public async ValueTask HandleAsync(GuildThreadCreateEventArgs arg)
    {
        // Discord raises THREAD_CREATE both for brand-new threads and when the bot merely gains visibility
        // of an existing one; only genuinely new threads carry newly_created, so ignore the rest.
        if (!arg.NewlyCreated)
        {
            return;
        }

        var thread = arg.Thread;
        var helpForumChannelId = options.Value.Discord.HelpForumChannelId;

        // Cheap pre-filter to avoid a REST round-trip for threads outside the configured help forum.
        if (helpForumChannelId is not { } forumId || thread.ParentId != forumId)
        {
            return;
        }

        // A forum thread's starter message rides along on the create event; its id equals the thread id,
        // so fall back to fetching it directly if the payload did not include it.
        var starter = thread is ForumGuildThread { Message: { } forumStarter }
            ? forumStarter
            : await rest.GetMessageAsync(thread.Id, thread.Id);

        var question = starter.Content.Trim();
        if (!ShouldAnswer(thread.ParentId, helpForumChannelId, starter.Author.IsBot, question))
        {
            return;
        }

        logger.AnsweringForumThread(thread.Id, starter.Author.Id);

        var answer = await answers.For(new(question), UserHash.For(starter.Author.Id), "discord-forum");
        var content = DiscordAnswers.Format(answer);

        // The 👍/👎 feedback buttons ride on the answer message; the standing note follows as its own message.
        if (answer.InteractionId is { } interactionId)
        {
            var sent = await rest.SendMessageAsync(thread.Id, new MessageProperties
            {
                Content = content,
                Components = [FeedbackButtonRow.For(interactionId)]
            });

            await interactionLog.SetAnswerMessage(interactionId, sent.Id.ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            await rest.SendMessageAsync(thread.Id, content);
        }

        await rest.SendMessageAsync(thread.Id, FollowUpNote);
    }
}
