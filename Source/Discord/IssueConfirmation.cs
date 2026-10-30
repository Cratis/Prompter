// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;
using Cratis.Prompter.Protection;
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
/// <param name="admission">The cheap shared scope and rate guard.</param>
/// <param name="options">The Prompter options carrying the GitHub configuration.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class IssueConfirmation(
    IIssues issues,
    PendingIssues pending,
    Admission admission,
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
        "GitHub did not confirm the result. An issue may have been created. Check the repository before drafting again; I will not retry this submission.";

    /// <summary>
    /// Files or discards a drafted issue. Any custom id whose first segment is
    /// <see cref="IssueButton.Prefix"/> routes here.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [ComponentInteraction(IssueButton.Prefix)]
    public async Task Confirm()
    {
        if (!CanConfirm())
        {
            return;
        }

        if (Context.Interaction is not MessageComponentInteraction interaction)
        {
            return;
        }

        var customId = Context.Interaction.Data.CustomId;
        var decision = admission.TryEnter(new(Context.Interaction.GuildId, Context.Channel.Id, Context.User.Id), customId);
        if (!decision.Allowed)
        {
            if (decision.Reply)
            {
                await Acknowledge(options.Value.Discord.RateLimitedReply);
            }

            return;
        }
        var click = IssueButton.Parse(customId);
        if (click is null)
        {
            logger.UnrecognizedIssueCustomId(customId);
            await Acknowledge(ExpiredReply);
            return;
        }

        var consent = new IssueConsent(Context.User.Id, Context.Interaction.GuildId!.Value, Context.Channel.Id);
        await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(options.Value.Discord.AnswerTimeoutSeconds));
        var reply = await Submit(click, consent, interaction.Message.Id, timeout.Token);
        await Context.Interaction.SendFollowupMessageAsync(new()
        {
            Content = reply,
            Flags = MessageFlags.Ephemeral,
            AllowedMentions = AllowedMentionsProperties.None
        });
    }

    /// <summary>
    /// Consumes one admitted preview action and submits its exact payload at most once.
    /// </summary>
    /// <param name="click">The parsed action.</param>
    /// <param name="consent">The admitted reporter and public scope.</param>
    /// <param name="message">The clicked preview message ID.</param>
    /// <param name="cancellationToken">Cancels the single submission attempt.</param>
    /// <returns>A truthful result; uncertain failures never invite an automatic retry.</returns>
    public async Task<string> Submit(IssueButtonClick click, IssueConsent consent, ulong message, CancellationToken cancellationToken)
    {
        // Taking removes the draft, so file and cancel race for the same single use.
        var held = pending.Take(click.Token, consent, message);
        if (held is null)
        {
            return ExpiredReply;
        }

        if (!click.Files)
        {
            return CancelledReply;
        }

        try
        {
            var filed = await issues.File(held, cancellationToken);

            logger.FiledIssueFromDiscord(filed.Repository, filed.Number);

            return $"Filed: {filed.Url}";
        }
        catch (Exception exception)
        {
            logger.IssueFilingFailed(exception, held.Repository);

            return FailedReply;
        }
    }

    bool CanConfirm() => options.Value.Protection.GitHubWritesEnabled && options.Value.GitHub.FilingEnabled && Context.Interaction.GuildId is not null;

    async Task Acknowledge(string content) =>
        await Context.Interaction.SendResponseAsync(
            InteractionCallback.Message(new() { Content = content, Flags = MessageFlags.Ephemeral, AllowedMentions = AllowedMentionsProperties.None }));
}
