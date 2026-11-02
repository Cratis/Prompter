// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace Cratis.Prompter.Discord;

/// <summary>The thin interaction adapter for consented forwards to the maintainer channel.</summary>
/// <param name="forwards">Forward authorization and delivery.</param>
/// <param name="actions">The shared nonpaid reservation and deadline.</param>
public class MaintainerForwardButtons(MaintainerForwards forwards, NonpaidActions actions) : ComponentInteractionModule<ButtonInteractionContext>
{
    /// <summary>Forwards a question the documentation could not answer.</summary>
    /// <param name="channelId">Question channel.</param>
    /// <param name="messageId">Question message.</param>
    /// <returns>The interaction completion.</returns>
    [ComponentInteraction(AnswerActionButton.DocumentedPrefix)]
    public Task Documented(ulong channelId, ulong messageId) =>
        Handle(ForwardKind.Documentation, AnswerActionButton.CustomId(AnswerActionButton.DocumentedPrefix, new(channelId, messageId)));

    /// <summary>Forwards a feature request.</summary>
    /// <param name="channelId">Question channel.</param>
    /// <param name="messageId">Question message.</param>
    /// <param name="product">The routing product key.</param>
    /// <returns>The interaction completion.</returns>
    [ComponentInteraction(AnswerActionButton.RequestPrefix)]
    public Task Request(ulong channelId, ulong messageId, string product) =>
        Handle(ForwardKind.FeatureRequest, AnswerActionButton.RequestCustomId(new(channelId, messageId), product));

    Task Handle(ForwardKind kind, string customId)
    {
        var actor = new DiscordActor(Context.Interaction.GuildId, Context.Channel.Id, Context.User.Id);
        var click = new ForwardClick(kind, actor, customId) { Channel = Context.Channel };
        return actions.Run(
            actor,
            Context.Channel,
            new(
                token => Context.Interaction.SendResponseAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral), cancellationToken: token),
                token => Context.Guild is null ? Task.FromResult(MaintainerForwards.UnavailableReply) : forwards.Forward(click, token),
                (reply, token) => Context.Interaction.SendResponseAsync(InteractionCallback.Message(SafeMessages.Ephemeral(reply)), cancellationToken: token),
                (reply, token) => Context.Interaction.SendFollowupMessageAsync(new() { Content = reply, Flags = MessageFlags.Ephemeral, AllowedMentions = AllowedMentionsProperties.None }, cancellationToken: token)));
    }
}
