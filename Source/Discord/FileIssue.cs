// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Prompter.GitHub;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace Cratis.Prompter.Discord;

/// <summary>
/// The <c>/issue</c> slash command: turns what someone is describing in Discord into a GitHub issue.
/// </summary>
/// <param name="drafting">Drafts the issue from the description.</param>
/// <param name="issues">Looks for likely duplicates before the preview is shown.</param>
/// <param name="pending">Holds the draft between the preview and the confirmation.</param>
/// <param name="rateLimiter">The per-user throttle, shared with question answering.</param>
/// <param name="timeProvider">The clock the throttle's refills are measured against.</param>
/// <param name="options">The Prompter options carrying the GitHub configuration and reply text.</param>
/// <param name="logger">Logger for diagnostics.</param>
/// <remarks>
/// Filing is always a deliberate act (decision D-16): Prompter drafts, the reporter confirms in an ephemeral
/// preview, and only then does an issue exist. Anyone may file — at this community's size an approval step
/// would cost more in missed reports than it saves in noise — with the duplicate hint, the shared rate limit
/// and the provenance label carrying that load instead.
/// </remarks>
public class FileIssue(
    IIssueDrafting drafting,
    IIssues issues,
    PendingIssues pending,
    RateLimiter rateLimiter,
    TimeProvider timeProvider,
    IOptions<PrompterOptions> options,
    ILogger<FileIssue> logger) : ApplicationCommandModule<ApplicationCommandContext>
{
    /// <summary>
    /// The reply when issue filing has no credential configured.
    /// </summary>
    public const string NotConfiguredReply =
        "I can't file issues yet — no GitHub credential is configured for me. A maintainer can set one up.";

    /// <summary>
    /// The reply when the model could not turn the description into an issue.
    /// </summary>
    public const string CouldNotDraftReply =
        "I couldn't turn that into an issue. Try describing what you expected and what happened instead.";

    /// <summary>
    /// Drafts a GitHub issue from a description and shows it for confirmation.
    /// </summary>
    /// <param name="description">What is wrong, missing, or being asked for.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [SlashCommand("issue", "Turn this into a GitHub issue on the right Cratis repository")]
    public async Task Handle(string description)
    {
        var github = options.Value.GitHub;

        if (!github.FilingEnabled)
        {
            await RespondEphemerally(NotConfiguredReply);
            return;
        }

        // The rate-limit key is the user id, used only in memory - never stored, never logged.
        var userKey = Context.User.Id.ToString(CultureInfo.InvariantCulture);
        if (!rateLimiter.TryConsume(userKey, timeProvider.GetUtcNow()))
        {
            logger.IssueRateLimited();
            await RespondEphemerally(options.Value.Discord.RateLimitedReply);
            return;
        }

        // Drafting calls the model, which takes seconds - well past the 3-second acknowledgement window.
        // The deferral is ephemeral so the whole exchange stays private until an issue actually exists.
        await Context.Interaction.SendResponseAsync(
            InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        try
        {
            var draft = await drafting.Draft(description, hint: null);
            if (draft is null)
            {
                logger.CouldNotDraftIssue();
                await Context.Interaction.SendFollowupMessageAsync(Ephemeral(CouldNotDraftReply));
                return;
            }

            var repository = IssueRouting.RepositoryFor(draft.Product, github);
            var routed = IssueRouting.IsRouted(draft.Product, github);
            var similar = await issues.FindSimilar(repository, draft.Title);
            var token = pending.Hold(draft, repository, ConversationUrl());

            await Context.Interaction.SendFollowupMessageAsync(new()
            {
                Content = IssuePreview.Text(draft, repository, routed, similar),
                Flags = MessageFlags.Ephemeral,
                Components = [IssuePreview.Buttons(token)]
            });
        }
        catch (Exception exception)
        {
            logger.IssueDraftingFailed(exception);
            await TryApologize();
        }
    }

    static InteractionMessageProperties Ephemeral(string content) =>
        new() { Content = content, Flags = MessageFlags.Ephemeral };

    string? ConversationUrl()
    {
        if (Context.Interaction.Channel is null)
        {
            return null;
        }

        var guild = Context.Interaction.GuildId?.ToString(CultureInfo.InvariantCulture) ?? "@me";
        var channel = Context.Interaction.Channel.Id.ToString(CultureInfo.InvariantCulture);

        return $"https://discord.com/channels/{guild}/{channel}";
    }

    async Task RespondEphemerally(string content) =>
        await Context.Interaction.SendResponseAsync(InteractionCallback.Message(Ephemeral(content)));

    async Task TryApologize()
    {
        try
        {
            await Context.Interaction.SendFollowupMessageAsync(Ephemeral(options.Value.Discord.ErrorReply));
        }
        catch (Exception exception)
        {
            // Nothing more can be delivered to this interaction; record it and let the command return.
            logger.IssueApologyFailed(exception);
        }
    }
}
