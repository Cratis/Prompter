// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Storage;
using Microsoft.Extensions.Logging;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace Cratis.Prompter.Discord;

/// <summary>
/// Handles clicks on the 👍/👎 feedback buttons attached to Prompter's answers, writing the verdict back to
/// the interaction row identified by the button's custom id.
/// </summary>
/// <param name="interactionLog">The log the verdict is written to.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class Feedback(IInteractionLog interactionLog, ILogger<Feedback> logger)
    : ComponentInteractionModule<ComponentInteractionContext>
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
        // Acknowledge first so the click never surfaces Discord's "interaction failed", even if the write
        // fails - feedback is best-effort. The ephemeral reply is visible only to the clicking user. This
        // runs well inside the 3-second interaction window.
        await Context.Interaction.SendResponseAsync(
            InteractionCallback.Message(new()
            {
                Content = Acknowledgement,
                Flags = MessageFlags.Ephemeral
            }));

        var customId = Context.Interaction.Data.CustomId;
        var click = FeedbackButton.Parse(customId);
        if (click is null)
        {
            logger.UnrecognizedFeedbackCustomId(customId);
            return;
        }

        await interactionLog.RecordFeedback(click.InteractionId, click.Verdict);
        logger.RecordedFeedback(click.Verdict.ToText(), click.InteractionId);
    }
}
