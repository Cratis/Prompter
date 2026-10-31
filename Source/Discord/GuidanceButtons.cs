// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Protection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace Cratis.Prompter.Discord;

/// <summary>
/// Handles clicks on follow-up suggestion and product clarification buttons by asking the resulting question through
/// the same admitted, leased and protected path as <c language="text">/ask</c>.
/// </summary>
/// <param name="assistance">The conversation assistance that answers the question.</param>
/// <param name="reader">Builds the bounded question context.</param>
/// <param name="admission">The public admission gate.</param>
/// <param name="work">The paid-work gate.</param>
/// <param name="options">The Prompter options.</param>
/// <param name="logger">Diagnostics without question content.</param>
public class GuidanceButtons(
    ConversationAssistance assistance,
    ConversationContextReader reader,
    Admission admission,
    PaidWork work,
    IOptions<PrompterOptions> options,
    ILogger<GuidanceButtons> logger) : ComponentInteractionModule<ButtonInteractionContext>
{
    /// <summary>The ephemeral notice when a suggestion button can no longer be resolved.</summary>
    public const string SuggestionUnavailable = "This suggestion is no longer available.";

    /// <summary>The ephemeral notice when a clarification can no longer be resolved.</summary>
    public const string ClarificationUnavailable = "I could not recover the original question. Please ask it again.";

    /// <summary>Asks the clicked follow-up suggestion as a new question.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [ComponentInteraction(SuggestionButton.Prefix)]
    public async Task Suggest()
    {
        var index = SuggestionButton.Parse(Context.Interaction.Data.CustomId);
        var buttons = Context.Interaction.Message.Components.OfType<ActionRow>().SelectMany(row => row.Components).OfType<Button>()
            .Where(button => button.CustomId is not null).Select(button => (button.CustomId, button.Label ?? string.Empty));
        var label = index is { } found ? SuggestionButton.LabelAt(buttons, found) : null;
        if (label is null)
        {
            await Notify(SuggestionUnavailable);
            return;
        }

        await Ask(label, "discord-suggestion", scope: null);
    }

    /// <summary>Re-asks the original question scoped to the clicked product.</summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    [ComponentInteraction(ClarificationButton.Prefix)]
    public async Task Clarify()
    {
        var product = ClarificationButton.Parse(Context.Interaction.Data.CustomId);
        var question = Clarifications.QuestionFrom(Context.Interaction.Message.Content);
        if (product is null || question is null)
        {
            await Notify(ClarificationUnavailable);
            return;
        }

        await Ask(question, "discord-clarification", product);
    }

    async Task Ask(string text, string source, InteractionProduct? scope)
    {
        var interaction = Context.Interaction;
        if (interaction.GuildId is not { } guildId)
        {
            await Notify(options.Value.Discord.RateLimitedReply);
            return;
        }

        var decision = admission.TryEnter(new(guildId, Context.Channel.Id, Context.User.Id), text);
        if (!decision.Allowed)
        {
            if (decision.Reply)
            {
                await Notify(options.Value.Discord.RateLimitedReply);
            }

            return;
        }

        var deferred = false;
        try
        {
            await using var lease = await work.BeginInteractive();
            lease.Activate();
            await interaction.SendResponseAsync(InteractionCallback.DeferredMessage());
            deferred = true;
            var location = new ConversationLocation(guildId, Context.Channel.Id, interaction.Id, interaction.CreatedAt);
            var anchor = new HistoryMessage(Context.Interaction.Message.Id, Context.Channel.Id, Context.Interaction.Message.CreatedAt, Context.Interaction.Message.Content, IsBot: true, IsSelf: true, IsSystem: false);
            var request = (await reader.For(ConversationText.Sanitize(text), location, anchor, lease.Token)) with { Scope = scope };
            var reply = await assistance.For(request, source, lease.Token);
            await InteractionAnswers.Send(interaction, reply, AnswerActions.For(options.Value.Discord, new(Context.Channel.Id, 0), reply));
        }
        catch (WorkRefused)
        {
            logger.RateLimited();
            if (admission.MayReply(new(guildId, Context.Channel.Id, Context.User.Id)))
            {
                await (deferred
                    ? interaction.SendFollowupMessageAsync(new() { Content = options.Value.Discord.RateLimitedReply, Flags = MessageFlags.Ephemeral })
                    : Notify(options.Value.Discord.RateLimitedReply));
            }
        }
        catch (Exception exception)
        {
            logger.AnswerFailed(exception);
            await TryApologize();
        }
    }

    async Task Notify(string content) => await Context.Interaction.SendResponseAsync(
        InteractionCallback.Message(new() { Content = content, Flags = MessageFlags.Ephemeral }));

    async Task TryApologize()
    {
        try
        {
            await Context.Interaction.SendFollowupMessageAsync(options.Value.Discord.ErrorReply);
        }
        catch (Exception exception)
        {
            logger.ApologyFailed(exception);
        }
    }
}
