// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ComponentInteractions;
using NetCord.Services.ComponentInteractions;

namespace Cratis.Prompter.Discord;

/// <summary>Silently rejects out-of-scope buttons before module execution or result handling.</summary>
public class ButtonAdmission : IComponentInteractionPreExecutionHandler<ButtonInteractionContext>
{
    /// <inheritdoc/>
    public ValueTask<PreExecutionResult> HandleAsync(ButtonInteractionContext context, GatewayClient? client, ILogger logger, IServiceProvider services)
    {
        var admission = services.GetRequiredService<Admission>();
        var actor = new DiscordActor(context.Interaction.GuildId, context.Channel.Id, context.User.Id);
        return ValueTask.FromResult(ChannelAdmission.InScope(admission, actor, context.Channel)
            ? PreExecutionResult.Continue
            : PreExecutionResult.Skip);
    }
}
