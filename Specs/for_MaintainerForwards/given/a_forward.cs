// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Digest;
using Cratis.Prompter.Discord;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Specs.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Rest;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_MaintainerForwards.given;

public class a_forward : Specification
{
    RestClient _rest = null!;

    protected PrompterOptions Options { get; private set; } = null!;
    protected FakeDiscordMessages Discord { get; private set; } = null!;
    protected IMaintainerChannel Channel { get; private set; } = null!;
    protected MaintainerForwards Forwards { get; private set; } = null!;
    protected NonpaidActions Actions { get; private set; } = null!;
    protected ForwardClick Documented { get; private set; } = null!;
    protected ForwardClick Requested { get; private set; } = null!;
    protected ControlledTime Clock { get; private set; } = null!;
    protected string Reply { get; set; } = null!;
    protected TextChannel ThreadChannel(ulong? parent) => TextChannel.CreateFromJson(new() { Id = 2, GuildId = 1, Type = ChannelType.PublicGuildThread, ParentId = parent }, _rest);

    protected Task Run(ForwardClick click) => Actions.Run(click.Actor, click.Channel, new(_ => Task.CompletedTask, token => Forwards.Forward(click, token), Capture, Capture), TestContext.Current.CancellationToken);

    Task Capture(string reply, CancellationToken token)
    {
        Reply = reply;
        return Task.CompletedTask;
    }

    void Destroy() => _rest.Dispose();

    void Establish()
    {
        var clock = Clock = new ControlledTime(new(2026, 6, 5, 12, 0, 0, TimeSpan.Zero));
        Options = new();
        Options.Protection.AllowedGuilds = [1];
        Options.Discord.AskChannelId = 2;
        Options.Discord.MaintainerChannelId = 99;
        var config = Microsoft.Extensions.Options.Options.Create(Options);
        Discord = new() { Author = 3, Content = "How do I <@123> configure the thing?" };
        _rest = Discord.Client();
        Channel = Substitute.For<IMaintainerChannel>();
        var admission = new Admission(config, new RateLimiter(config), clock);
        Forwards = new(_rest, Channel, admission, config, clock, NullLogger<MaintainerForwards>.Instance);
        Actions = new(admission, config, clock);
        var question = new QuestionLink(2, 4);
        var channel = TextChannel.CreateFromJson(new() { Id = 2, GuildId = 1, Type = ChannelType.TextGuildChannel }, _rest);
        Documented = new(ForwardKind.Documentation, new(1, 2, 3), AnswerActionButton.CustomId(AnswerActionButton.DocumentedPrefix, question)) { Channel = channel };
        Requested = new(ForwardKind.FeatureRequest, new(1, 2, 3), AnswerActionButton.RequestCustomId(question, "chronicle")) { Channel = channel };
    }
}
