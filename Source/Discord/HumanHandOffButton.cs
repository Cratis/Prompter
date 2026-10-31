// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace Cratis.Prompter.Discord;

/// <summary>The thin interaction adapter for explicit human hand-offs.</summary>
/// <param name="handOffs">Hand-off authorization and delivery.</param>
/// <param name="options">The answer timeout.</param>
public class HumanHandOffButton(HumanHandOffs handOffs, IOptions<PrompterOptions> options) : ComponentInteractionModule<ButtonInteractionContext>
{
    /// <summary>Defers before any REST access, then requests a human.</summary>
    /// <param name="channelId">Question channel.</param>
    /// <param name="messageId">Question message or zero for a command.</param>
    /// <returns>The interaction completion.</returns>
    [ComponentInteraction(AnswerActionButton.HumanPrefix)]
    public async Task Handle(ulong channelId, ulong messageId)
    {
        if (Context.Guild is null)
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = HumanHandOffs.UnavailableReply, Flags = MessageFlags.Ephemeral, AllowedMentions = AllowedMentionsProperties.None }));
            return;
        }
        await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));
        var question = new QuestionLink(channelId, messageId);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(options.Value.Discord.AnswerTimeoutSeconds));
        var click = new HandOffClick(
            new(Context.Guild.Id, Context.Channel.Id, Context.User.Id),
            AnswerActionButton.CustomId(AnswerActionButton.HumanPrefix, question),
            Conversation(question),
            Context.Message.Id,
            Context.Message.InteractionMetadata?.User.Id);
        var reply = await handOffs.Request(click, timeout.Token);
        await Context.Interaction.SendFollowupMessageAsync(new() { Content = reply, Flags = MessageFlags.Ephemeral, AllowedMentions = AllowedMentionsProperties.None });
    }

    ulong Conversation(QuestionLink question)
    {
        if (Context.Channel is GuildThread)
        {
            return Context.Channel.Id;
        }

        return question.HasMessage ? question.MessageId : Context.Message.Id;
    }
}
