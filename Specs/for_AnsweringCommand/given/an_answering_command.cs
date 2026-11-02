// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reflection;
using System.Text.Json;
using Cratis.Prompter.Discord;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using NetCord;
using NetCord.Gateway;
using NetCord.JsonModels;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_AnsweringCommand.given;

public class an_answering_command : Specification
{
    protected const ulong Role = 10;
    protected const ulong Guild = 1;
    GatewayClient _client = null!;

    protected AnsweringSwitch Switch { get; private set; } = null!;
    protected IAnsweringPauses Pauses { get; private set; } = null!;
    protected PrompterOptions Options { get; private set; } = null!;
    protected IList<string> Payloads { get; } = [];
    protected IList<string> Paths { get; } = [];
    protected AnsweringCommand Command { get; private set; } = null!;

    protected void Invoke(ulong guild, params ulong[] roles)
    {
        var rolesJson = string.Join(',', roles);
        var envelope = JsonSerializer.Deserialize<JsonInteraction>(
            $$"""{"id":100,"application_id":42,"type":2,"token":"test","entitlements":[],"guild_id":{{guild}},"channel":{"id":70,"guild_id":{{guild}},"type":0},"member":{"user":{"id":3,"username":"requester"},"roles":[{{rolesJson}}]},"data":{"id":8,"name":"answering","type":1} }""")!;
        envelope.User = envelope.GuildUser!.User;
        var context = new ApplicationCommandContext(
            new SlashCommandInteraction(envelope, null, (interaction, response, withResponse, properties, token) => _client.Rest.SendInteractionResponseAsync(interaction.Id, interaction.Token, response, withResponse, properties, token), _client.Rest),
            _client);
        typeof(ApplicationCommandModule<ApplicationCommandContext>).BaseType!.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(method => method.Name.EndsWith(".SetContext", StringComparison.Ordinal)).Invoke(Command, [context]);
    }

    protected bool Replied(string text) => Payloads.Any(payload => payload.Contains(text, StringComparison.Ordinal));

    void Establish()
    {
        Options = new();
        Options.Protection.AllowedGuilds = [Guild];
        Options.Discord.MaintainerRoleId = Role;
        Switch = Fakes.Switch.Running();
        Pauses = Substitute.For<IAnsweringPauses>();
        var token = Substitute.For<IEntityToken>();
        token.Id.Returns(42UL);
        var handler = Substitute.For<IRestRequestHandler>();
        handler.SendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            var request = call.Arg<HttpRequestMessage>();
            Paths.Add(request.RequestUri!.AbsolutePath);
            if (request.Content is not null)
            {
                Payloads.Add(await request.Content.ReadAsStringAsync());
            }

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"id":200,"channel_id":70,"author":{"id":42,"username":"bot","bot":true},"content":"ok","timestamp":"2026-01-01T00:00:00Z","mentions":[],"mention_roles":[],"attachments":[],"embeds":[],"components":[]}""") };
        });
        _client = new(token, new() { RestClientConfiguration = new() { RequestHandler = handler } });
        Command = new(Switch, Pauses, Microsoft.Extensions.Options.Options.Create(Options), NullLogger<AnsweringCommand>.Instance);
    }

    void Destroy() => _client.Dispose();
}
