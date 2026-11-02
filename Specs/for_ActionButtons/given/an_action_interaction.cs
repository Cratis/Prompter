// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Prompter.Digest;
using Cratis.Prompter.Discord;
using Cratis.Prompter.Protection;
using Microsoft.Extensions.Logging.Abstractions;
using NetCord;
using NetCord.Gateway;
using NetCord.Gateway.JsonModels;
using NetCord.JsonModels;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.ComponentInteractions;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ActionButtons.given;

public class an_action_interaction : for_AskPrompter.given.an_interaction
{
    protected ButtonInteractionContext ButtonContext { get; private set; } = null!;
    protected IMaintainerChannel Channel { get; private set; } = null!;

    protected Func<IInteraction, InteractionCallbackProperties, bool, RestRequestProperties?, CancellationToken, Task<InteractionCallbackResponse?>> Response =>
        (request, response, withResponse, properties, token) => Client.Rest.SendInteractionResponseAsync(request.Id, request.Token, response, withResponse, properties, token);

    protected static void SetContext(object module, Type moduleType, object context) => moduleType.BaseType!.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
        .Single(method => method.Name.EndsWith(".SetContext", StringComparison.Ordinal)).Invoke(module, [context]);

    protected void CreateContext(ulong? parent, string customId = "suggest:0")
    {
        Button(thread: true);
        var guild = ImmutableGatewayClientCacheProvider.FromJson(new JsonGatewayClientCache
        {
            User = new() { Id = 42, Username = "bot", IsBot = true },
            Guilds =
            [
                new()
                {
                    Id = 1, Name = "guild", Roles = [], Channels = [], ActiveThreads = [], Users = [],
                    VoiceStates = [], Presences = [], Emojis = [], Features = [], StageInstances = [], Stickers = [], ScheduledEvents = []
                }
            ]
        }).Create(42, Client.Rest).Guilds[1];
        var json = ((IJsonModel<JsonInteraction>)Buttons.Context.Interaction).JsonModel;
        json.Channel!.ParentId = parent;
        json.Data!.CustomId = customId;
        ButtonContext = new(new ButtonInteraction(json, guild, Response, Client.Rest), Client);
        Channel = Substitute.For<IMaintainerChannel>();
    }

    protected void ForgetGuild()
    {
        var json = ((IJsonModel<JsonInteraction>)ButtonContext.Interaction).JsonModel;
        ButtonContext = new(new ButtonInteraction(json, null, Response, Client.Rest), Client);
    }

    protected void Bind(ComponentInteractionModule<ButtonInteractionContext> module) =>
        SetContext(module, typeof(ComponentInteractionModule<ButtonInteractionContext>), ButtonContext);

    protected void Bind(ApplicationCommandModule<ApplicationCommandContext> module)
    {
        var json = ((IJsonModel<JsonInteraction>)ButtonContext.Interaction).JsonModel;
        json.Type = InteractionType.ApplicationCommand;
        json.Data!.Type = ApplicationCommandType.ChatInput;
        json.Data.Name = "issue";
        var context = new ApplicationCommandContext(new SlashCommandInteraction(json, ButtonContext.Guild, Response, Client.Rest), Client);
        SetContext(module, typeof(ApplicationCommandModule<ApplicationCommandContext>), context);
    }

    protected async Task ClickActions()
    {
        Options.Discord.MaintainerRoleId = 9;
        Options.Discord.MaintainerChannelId = 99;
        var config = Microsoft.Extensions.Options.Options.Create(Options);
        var actions = new NonpaidActions(Admission, config, Clock);
        var human = new HumanHandOffButton(new(Client.Rest, Admission, new(config, Clock), config, Clock, NullLogger<HumanHandOffs>.Instance), actions);
        var forward = new MaintainerForwardButtons(new(Client.Rest, Channel, Admission, config, Clock, NullLogger<MaintainerForwards>.Instance), actions);
        Bind(human);
        Bind(forward);
        await human.Handle(70, 90);
        await forward.Documented(70, 90);
        await forward.Request(70, 90, "chronicle");
    }
}
