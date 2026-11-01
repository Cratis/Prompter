// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Cratis.Prompter.Digest;
using Cratis.Prompter.Discord;
using Microsoft.Extensions.Logging.Abstractions;
using NetCord;
using NetCord.Gateway;
using NetCord.Gateway.JsonModels;
using NetCord.JsonModels;
using NetCord.Services.ComponentInteractions;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ActionButtons.when_authorizing;

public class and_the_interaction_parent_is_denied : for_AskPrompter.given.an_interaction
{
    IMaintainerChannel _channel = null!;

    async Task Because()
    {
        Options.Discord.MaintainerRoleId = 9;
        Options.Discord.MaintainerChannelId = 99;
        Options.Protection.DeniedChannels = [7];
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
        var interaction = new ButtonInteraction(
            json,
            guild,
            (request, response, withResponse, properties, token) => Client.Rest.SendInteractionResponseAsync(request.Id, request.Token, response, withResponse, properties, token),
            Client.Rest);
        var context = new ButtonInteractionContext(interaction, Client);
        var config = Microsoft.Extensions.Options.Options.Create(Options);
        _channel = Substitute.For<IMaintainerChannel>();
        var human = new HumanHandOffButton(new(Client.Rest, Admission, new(config, Clock), config, Clock, NullLogger<HumanHandOffs>.Instance), config);
        var forward = new MaintainerForwardButtons(new(Client.Rest, _channel, Admission, config, Clock, NullLogger<MaintainerForwards>.Instance), config);
        var setContext = typeof(ComponentInteractionModule<ButtonInteractionContext>).BaseType!.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(method => method.Name.EndsWith(".SetContext", StringComparison.Ordinal));
        setContext.Invoke(human, [context]);
        setContext.Invoke(forward, [context]);
        await human.Handle(70, 90);
        await forward.Documented(70, 90);
        await forward.Request(70, 90, "chronicle");
    }

    [Fact] void should_not_fetch_or_post_in_any_channel() => Paths.Any(path => path.Contains("/channels/", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_forward_to_maintainers() => _channel.DidNotReceiveWithAnyArgs().Post(default, string.Empty, TestContext.Current.CancellationToken);
    [Fact] void should_send_only_rejection_followups() => Payloads.Count(payload => JsonSerializer.Deserialize<JsonElement>(payload).TryGetProperty("content", out var content) && content.GetString() == Options.Discord.RateLimitedReply).ShouldEqual(3);
}
