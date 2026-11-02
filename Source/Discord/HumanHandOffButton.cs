// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace Cratis.Prompter.Discord;

/// <summary>The thin interaction adapter for explicit human hand-offs.</summary>
/// <param name="handOffs">Hand-off authorization and delivery.</param>
/// <param name="actions">The shared nonpaid reservation and deadline.</param>
public class HumanHandOffButton(HumanHandOffs handOffs, NonpaidActions actions) : ComponentInteractionModule<ButtonInteractionContext>
{
    /// <summary>Defers before any REST access, then requests a human.</summary>
    /// <param name="channelId">Question channel.</param>
    /// <param name="messageId">Question message or zero for a command.</param>
    /// <returns>The interaction completion.</returns>
    [ComponentInteraction(AnswerActionButton.HumanPrefix)]
    public Task Handle(ulong channelId, ulong messageId)
    {
        var actor = new DiscordActor(Context.Interaction.GuildId, Context.Channel.Id, Context.User.Id);
        var question = new QuestionLink(channelId, messageId);
        var click = new HandOffClick(actor, AnswerActionButton.CustomId(AnswerActionButton.HumanPrefix, question), Conversation(question), Context.Message.Id, Context.Message.InteractionMetadata?.User.Id)
        {
            Channel = Context.Channel
        };
        return actions.Run(
            actor,
            Context.Channel,
            new(
                token => Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral), cancellationToken: token),
                token => Context.Guild is null ? Task.FromResult(HumanHandOffs.UnavailableReply) : handOffs.Request(click, token),
                (reply, token) => Context.Interaction.SendResponseAsync(InteractionCallback.Message(SafeMessages.Ephemeral(reply)), cancellationToken: token),
                (reply, token) => Context.Interaction.SendFollowupMessageAsync(new() { Content = reply, Flags = MessageFlags.Ephemeral, AllowedMentions = AllowedMentionsProperties.None }, cancellationToken: token)));
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
