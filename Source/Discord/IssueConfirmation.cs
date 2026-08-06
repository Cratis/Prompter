// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace Cratis.Prompter.Discord;

/// <summary>
/// Handles the buttons under an issue preview: files the held draft, or discards it.
/// </summary>
/// <param name="issues">Files the issue.</param>
/// <param name="pending">Holds the drafts awaiting confirmation.</param>
/// <param name="options">The Prompter options carrying the GitHub configuration.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class IssueConfirmation(
    IIssues issues,
    PendingIssues pending,
    IOptions<PrompterOptions> options,
    ILogger<IssueConfirmation> logger) : ComponentInteractionModule<ComponentInteractionContext>
{
    /// <summary>
    /// The reply when the draft is no longer available to file.
    /// </summary>
    public const string ExpiredReply =
        "That draft has expired — run `/issue` again and I'll draft a fresh one.";

    /// <summary>
    /// The reply when the reporter discards the draft.
    /// </summary>
    public const string CancelledReply = "Nothing filed.";

    /// <summary>
    /// The reply when filing failed at GitHub.
    /// </summary>
    public const string FailedReply =
        "I couldn't file that on GitHub. The draft is gone, but nothing was created — try again in a moment.";

    /// <summary>
    /// Files or discards a drafted issue. Any custom id whose first segment is
    /// <see cref="IssueButton.Prefix"/> routes here.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [ComponentInteraction(IssueButton.Prefix)]
    public async Task Confirm()
    {
        var customId = Context.Interaction.Data.CustomId;
        var click = IssueButton.Parse(customId);
        if (click is null)
        {
            logger.UnrecognizedIssueCustomId(customId);
            await Acknowledge(ExpiredReply);
            return;
        }

        // Taking removes the draft, so a double-click cannot file the same issue twice.
        var held = pending.Take(click.Token);

        if (!click.Files)
        {
            await Acknowledge(CancelledReply);
            return;
        }

        if (held is null)
        {
            await Acknowledge(ExpiredReply);
            return;
        }

        // Filing calls GitHub, which is well within the interaction window but not guaranteed to be, so the
        // response is deferred first. The reply carries the issue URL, which is the only thing the reporter
        // needs from this exchange.
        await Context.Interaction.SendResponseAsync(
            InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));

        try
        {
            var filed = await issues.File(
                held.Repository,
                held.Draft.Title,
                IssueComposition.Body(held.Draft, held.ConversationUrl),
                IssueComposition.Labels(held.Draft, options.Value.GitHub));

            logger.FiledIssueFromDiscord(filed.Repository, filed.Number);

            await Context.Interaction.SendFollowupMessageAsync(new()
            {
                Content = $"Filed: {filed.Url}",
                Flags = MessageFlags.Ephemeral
            });
        }
        catch (Exception exception)
        {
            logger.IssueFilingFailed(exception, held.Repository);

            await Context.Interaction.SendFollowupMessageAsync(new()
            {
                Content = FailedReply,
                Flags = MessageFlags.Ephemeral
            });
        }
    }

    async Task Acknowledge(string content) =>
        await Context.Interaction.SendResponseAsync(
            InteractionCallback.Message(new() { Content = content, Flags = MessageFlags.Ephemeral }));
}
