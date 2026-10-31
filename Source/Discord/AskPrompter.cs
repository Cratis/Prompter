// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace Cratis.Prompter.Discord;

/// <summary>Answers a selected guild message publicly, with request-only thread context.</summary>
/// <param name="assistance">Documentation answers and separate advisory hints.</param>
/// <param name="admission">The shared cheap admission guard.</param>
/// <param name="work">The concurrency and durable spend guard.</param>
/// <param name="reader">The bounded, sanitizing context reader.</param>
/// <param name="delivery">Public answer delivery with optional threads.</param>
/// <param name="options">Protection and Discord reply configuration.</param>
/// <param name="logger">Diagnostics without message content.</param>
public class AskPrompter(
    ConversationAssistance assistance,
    Admission admission,
    PaidWork work,
    ConversationContextReader reader,
    AnswerDelivery delivery,
    IOptions<PrompterOptions> options,
    ILogger<AskPrompter> logger) : ApplicationCommandModule<ApplicationCommandContext>
{
    const string OnlyInServers = "Ask Prompter is only available in servers.";
    const string EmptySelection = "Select a message with plain text to ask Prompter.";
    const string LongSelection = "That message is too long to ask Prompter. Select a shorter message.";
    const string BotSelection = "Select a message written by a person, not a bot.";
    const string SystemSelection = "System messages cannot be answered. Select a message written by a person.";

    /// <summary>Answers the selected message after admission and under a paid-work lease.</summary>
    /// <returns>The asynchronous command operation.</returns>
    [MessageCommand("Ask Prompter", Contexts = [InteractionContextType.Guild])]
    public async Task Handle()
    {
        var target = ((MessageCommandInteraction)Context.Interaction).Data.TargetMessage;
        if (Context.Interaction.GuildId is not { } guild)
        {
            await Acknowledge(OnlyInServers, false);
            return;
        }

        var verdict = MessageSelection.For(
            target.Content,
            target.Author.IsBot,
            target.Type is not MessageType.Default and not MessageType.Reply,
            Math.Min(options.Value.Protection.MaxQuestionCharacters, ConversationContextReader.MaximumCurrentCharacters));
        if (verdict != MessageSelectionVerdict.Accept)
        {
            await Acknowledge(Explanation(verdict), false);
            return;
        }

        var decision = admission.TryEnter(new(guild, Context.Channel.Id, Context.User.Id), target.Content);
        if (!decision.Allowed)
        {
            if (decision.Reply) await Acknowledge(options.Value.Discord.RateLimitedReply, false);
            return;
        }

        var deferred = false;
        try
        {
            await using var lease = await work.BeginInteractive();
            lease.Activate();
            await Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));
            deferred = true;
            var inThread = Context.Channel is GuildThread;
            var question = await reader.ForSelection(target.Content, new(guild, target.ChannelId, target.Id, target.CreatedAt), inThread, lease.Token);
            var reply = await assistance.For(question, "discord-message-command", lease.Token);
            await delivery.Deliver(target, guild, inThread, target.Content, reply, lease.Token);
            await Acknowledge("Answered.", true);
        }
        catch (WorkRefused)
        {
            await RefuseAfterAdmission(deferred);
        }
        catch (Exception exception)
        {
            logger.AnswerFailed(exception);
            await TryApologize(deferred);
        }
    }

    static string Explanation(MessageSelectionVerdict verdict) => verdict switch
    {
        MessageSelectionVerdict.Empty => EmptySelection,
        MessageSelectionVerdict.TooLong => LongSelection,
        MessageSelectionVerdict.FromBot => BotSelection,
        _ => SystemSelection
    };

    async Task RefuseAfterAdmission(bool deferred)
    {
        if (admission.MayReply(new(Context.Interaction.GuildId, Context.Channel.Id, Context.User.Id)))
        {
            await Acknowledge(options.Value.Discord.RateLimitedReply, deferred);
        }
    }

    async Task Acknowledge(string content, bool deferred)
    {
        var message = SafeMessages.Followup(content);
        message.Flags = MessageFlags.Ephemeral;
        if (deferred)
        {
            await Context.Interaction.SendFollowupMessageAsync(message);
        }
        else
        {
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(message));
        }
    }

    async Task TryApologize(bool deferred)
    {
        try
        {
            await Acknowledge(options.Value.Discord.ErrorReply, deferred);
        }
        catch (Exception exception)
        {
            logger.ApologyFailed(exception);
        }
    }
}
