// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;

namespace Cratis.Prompter.Discord;

/// <summary>The thin interaction adapter for consented forwards to the maintainer channel.</summary>
/// <param name="forwards">Forward authorization and delivery.</param>
/// <param name="options">The answer timeout.</param>
public class MaintainerForwardButtons(MaintainerForwards forwards, IOptions<PrompterOptions> options) : ComponentInteractionModule<ButtonInteractionContext>
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

    async Task Handle(ForwardKind kind, string customId)
    {
        if (Context.Guild is null)
        {
            await RespondAsync(InteractionCallback.Message(new() { Content = MaintainerForwards.UnavailableReply, Flags = MessageFlags.Ephemeral, AllowedMentions = AllowedMentionsProperties.None }));
            return;
        }
        await RespondAsync(InteractionCallback.DeferredMessage(MessageFlags.Ephemeral));
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(options.Value.Discord.AnswerTimeoutSeconds));
        var click = new ForwardClick(kind, new(Context.Guild.Id, Context.Channel.Id, Context.User.Id), customId);
        var reply = await forwards.Forward(click, timeout.Token);
        await Context.Interaction.SendFollowupMessageAsync(new() { Content = reply, Flags = MessageFlags.Ephemeral, AllowedMentions = AllowedMentionsProperties.None });
    }
}
