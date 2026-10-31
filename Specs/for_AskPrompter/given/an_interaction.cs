// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Reflection;
using System.Text.Json;
using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;
using Cratis.Prompter.GitHub;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using NetCord;
using NetCord.Gateway;
using NetCord.JsonModels;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.ComponentInteractions;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_AskPrompter.given;

public class an_interaction : for_PaidWork.given.a_paid_work
{
    protected GatewayClient Client { get; private set; } = null!;
    protected IAnswers Answers { get; private set; } = null!;
    protected IConversationHistory History { get; private set; } = null!;
    protected Admission Admission { get; private set; } = null!;
    protected IList<string> Payloads { get; } = [];
    protected IList<string> Paths { get; } = [];
    protected AskPrompter Command { get; private set; } = null!;
    protected GuidanceButtons Buttons { get; private set; } = null!;
    protected Ask Slash { get; private set; } = null!;
    protected JsonInteraction Envelope { get; private set; } = null!;

    Func<IInteraction, InteractionCallbackProperties, bool, RestRequestProperties?, CancellationToken, Task<InteractionCallbackResponse?>> Response =>
        (interaction, response, withResponse, properties, token) => Client.Rest.SendInteractionResponseAsync(interaction.Id, interaction.Token, response, withResponse, properties, token);

    protected void Select(ulong? guild = 1, bool thread = false, bool bot = false, MessageType type = MessageType.Default)
    {
        Envelope = JsonSerializer.Deserialize<JsonInteraction>(
            """{"id":100,"application_id":42,"type":2,"token":"test","entitlements":[],"guild_id":1,"channel":{"id":70,"guild_id":1,"type":0},"member":{"user":{"id":3,"username":"requester"},"roles":[]},"data":{"id":8,"name":"Ask Prompter","type":3,"target_id":90,"resolved":{"messages":{"90":{"id":90,"channel_id":70,"author":{"id":4,"username":"author"},"content":"How do I handle commands?","timestamp":"2026-01-01T00:00:00Z","mentions":[],"mention_roles":[],"attachments":[],"embeds":[],"components":[]}}}}}""")!;
        Envelope.GuildId = guild;
        Envelope.User = Envelope.GuildUser!.User;
        Envelope.Channel!.Type = thread ? ChannelType.PublicGuildThread : ChannelType.TextGuildChannel;
        Envelope.Channel.ParentId = 7;
        var target = Envelope.Data!.ResolvedData!.Messages![90];
        target.Author!.IsBot = bot;
        target.Type = type;
        var context = new ApplicationCommandContext(new MessageCommandInteraction(Envelope, null, Response, Client.Rest), Client);
        typeof(ApplicationCommandModule<ApplicationCommandContext>).BaseType!.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(method => method.Name.EndsWith(".SetContext", StringComparison.Ordinal)).Invoke(Command, [context]);
        typeof(ApplicationCommandModule<ApplicationCommandContext>).BaseType!.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(method => method.Name.EndsWith(".SetContext", StringComparison.Ordinal)).Invoke(Slash, [context]);
    }

    protected void Button(string content = "An answer", ulong? guild = 1, bool thread = false, string customId = "suggest:0")
    {
        var json = JsonSerializer.Deserialize<JsonInteraction>(
            """{"id":101,"application_id":42,"type":3,"token":"test","entitlements":[],"guild_id":1,"channel":{"id":70,"guild_id":1,"type":0},"member":{"user":{"id":3,"username":"requester"},"roles":[]},"data":{"custom_id":"suggest:0","component_type":2},"message":{"id":90,"channel_id":70,"author":{"id":42,"username":"bot","bot":true},"content":"An answer","timestamp":"2026-01-01T00:00:00Z","mentions":[],"mention_roles":[],"attachments":[],"embeds":[],"components":[]}}""")!;
        json.GuildId = guild;
        json.User = json.GuildUser!.User;
        json.Channel!.Type = thread ? ChannelType.PublicGuildThread : ChannelType.TextGuildChannel;
        json.Channel.ParentId = 7;
        json.Message!.Content = content;
        json.Data!.CustomId = customId;
        var context = new ButtonInteractionContext(new ButtonInteraction(json, null, Response, Client.Rest), Client);
        typeof(ComponentInteractionModule<ButtonInteractionContext>).BaseType!.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(method => method.Name.EndsWith(".SetContext", StringComparison.Ordinal)).Invoke(Buttons, [context]);
    }

    protected void NoPaidWork()
    {
        Answers.ReceivedCalls().Any().ShouldBeFalse();
        History.ReceivedCalls().Any().ShouldBeFalse();
        Ledger.ReceivedCalls().Any().ShouldBeFalse();
    }

    void Establish()
    {
        Options.Protection.AllowedGuilds = [1];
        var config = Microsoft.Extensions.Options.Options.Create(Options);
        Admission = new(config, new RateLimiter(config), Clock);
        var token = Substitute.For<IEntityToken>();
        token.Id.Returns(42UL);
        var handler = Substitute.For<IRestRequestHandler>();
        handler.SendAsync(Arg.Any<HttpRequestMessage>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            var request = call.Arg<HttpRequestMessage>();
            Paths.Add(request.RequestUri!.AbsolutePath);
            if (request.Content is not null) Payloads.Add(await request.Content.ReadAsStringAsync());
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":200,"channel_id":70,"author":{"id":42,"username":"bot","bot":true},"content":"Answer","timestamp":"2026-01-01T00:00:00Z","mentions":[],"mention_roles":[],"attachments":[],"embeds":[],"components":[]}""")
            };
        });
        Client = new(token, new() { RestClientConfiguration = new() { RequestHandler = handler } });
        Answers = Substitute.For<IAnswers>();
        History = Substitute.For<IConversationHistory>();
        var assistance = new ConversationAssistance(Substitute.For<IRequestClassifier>(), Answers, Substitute.For<IRelatedIssues>(), config);
        var reader = new ConversationContextReader(History, Clock);
        Command = new(assistance, Admission, Work, reader, new(Client.Rest, config, NullLogger<AnswerDelivery>.Instance), config, NullLogger<AskPrompter>.Instance);
        Buttons = new(assistance, reader, Admission, Work, config, NullLogger<GuidanceButtons>.Instance);
        Slash = new(assistance, Admission, Work, config, NullLogger<Ask>.Instance);
    }

    void Destroy() => Client.Dispose();
}
