// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace Cratis.Prompter.Discord;

/// <summary>
/// Handles clicks on the 👍/👎 feedback buttons attached to Prompter's answers, writing the verdict back to
/// the interaction row identified by the button's custom id. Clicks are bounded: each user has a click
/// budget, and repeating the latest verdict for an answer is acknowledged silently without a write.
/// </summary>
/// <param name="interactionLog">The log the verdict is written to.</param>
/// <param name="votes">The per-user click budget and latest verdict per answer.</param>
/// <param name="admission">The shared refusal reply limiter.</param>
/// <param name="options">The Prompter options, for the refusal reply text.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class Feedback(IInteractionLog interactionLog, FeedbackVotes votes, Admission admission, IOptions<PrompterOptions> options, ILogger<Feedback> logger)
    : ComponentInteractionModule<ButtonInteractionContext>
{
    /// <summary>
    /// The ephemeral acknowledgement shown to a user after they click a feedback button.
    /// </summary>
    public const string Acknowledgement = "Thanks for the feedback!";

    /// <summary>
    /// Records the verdict from a clicked feedback button. Any custom id whose first segment is
    /// <see cref="FeedbackButton.Prefix"/> routes here.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [ComponentInteraction(FeedbackButton.Prefix)]
    public async Task Vote()
    {
        if (!votes.TrySpend(Context.User.Id))
        {
            await Refuse();
            return;
        }

        var customId = Context.Interaction.Data.CustomId;
        var click = FeedbackButton.Parse(customId);
        if (click is null)
        {
            await Acknowledge();
            logger.UnrecognizedFeedbackCustomId(customId);
            return;
        }

        switch (votes.Remember(click))
        {
            case FeedbackVote.Repeat:
                await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredModifyMessage);
                logger.FeedbackRepeated(click.InteractionId);
                return;
            case FeedbackVote.Refused:
                await Refuse();
                return;
        }

        await Acknowledge();
        await Record(click);
    }

    /// <summary>
    /// Acknowledges first so the click never surfaces Discord's "interaction failed", even if the write
    /// fails - feedback is best-effort. The ephemeral reply is visible only to the clicking user. This
    /// runs well inside the 3-second interaction window.
    /// </summary>
    async Task Acknowledge() => await Context.Interaction.SendResponseAsync(
        InteractionCallback.Message(new()
        {
            Content = Acknowledgement,
            Flags = MessageFlags.Ephemeral
        }));

    async Task Refuse()
    {
        logger.FeedbackClickRefused();
        if (admission.MayReply(new(Context.Interaction.GuildId, Context.Channel.Id, Context.User.Id)))
        {
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message(SafeMessages.Ephemeral(options.Value.Discord.RateLimitedReply)));
        }
    }

    async Task Record(FeedbackClick click)
    {
        try
        {
            // Last-writer-wins: overwriting any prior verdict is intentional — any viewer of the answer may
            // vote, and this is best-effort community signal, not an authored judgement, so no author check
            // is needed (Discord only dispatches interactions for custom ids on messages the user can see).
            await interactionLog.RecordFeedback(click.InteractionId, click.Verdict);
            logger.RecordedFeedback(click.Verdict.ToText(), click.InteractionId);
        }
        catch (Exception exception)
        {
            // The user was already told "thanks"; a blip writing the vote is best-effort, so log and move on
            // rather than letting the exception escape the handler after a successful acknowledgement. The
            // remembered verdict is forgotten so that a retry is written.
            votes.Forget(click);
            logger.FeedbackWriteFailed(exception, click.Verdict.ToText(), click.InteractionId);
        }
    }
}
