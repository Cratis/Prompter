// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace Cratis.Prompter.Discord;

/// <summary>Supplies transport callbacks for globally bounded feedback processing.</summary>
/// <param name="processing">The bounded feedback lifecycle.</param>
/// <param name="admission">The shared refusal reply limiter.</param>
/// <param name="options">The refusal reply text.</param>
/// <param name="logger">Content-free diagnostics.</param>
public class Feedback(FeedbackProcessing processing, Admission admission, IOptions<PrompterOptions> options, ILogger<Feedback> logger)
    : ComponentInteractionModule<ButtonInteractionContext>
{
    /// <summary>The ephemeral acknowledgement shown after a feedback click.</summary>
    public const string Acknowledgement = "Thanks for the feedback!";

    /// <summary>Processes every feedback click, including malformed identifiers, through the same budget.</summary>
    /// <returns>The bounded feedback lifecycle.</returns>
    [ComponentInteraction(FeedbackButton.Prefix)]
    public Task Vote() => processing.Run(Context.User.Id, Context.Interaction.Data.CustomId, new(Acknowledge, Repeat, Refuse));

    async Task Acknowledge(CancellationToken cancellationToken) => await Context.Interaction.SendResponseAsync(
        InteractionCallback.Message(new()
        {
            Content = Acknowledgement,
            Flags = MessageFlags.Ephemeral
        }),
        cancellationToken: cancellationToken);

    async Task Repeat(CancellationToken cancellationToken) => await Context.Interaction.SendResponseAsync(
        InteractionCallback.DeferredModifyMessage, cancellationToken: cancellationToken);

    async Task Refuse(CancellationToken cancellationToken)
    {
        logger.FeedbackClickRefused();
        if (admission.MayReply(new(Context.Interaction.GuildId, Context.Channel.Id, Context.User.Id)))
        {
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message(SafeMessages.Ephemeral(options.Value.Discord.RateLimitedReply)), cancellationToken: cancellationToken);
        }
    }
}
