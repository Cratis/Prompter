// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Digest;
using Cratis.Prompter.Discord;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Specs.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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
    protected ForwardClick Documented { get; private set; } = null!;
    protected ForwardClick Requested { get; private set; } = null!;
    protected ControlledTime Clock { get; private set; } = null!;
    protected string Reply { get; set; } = null!;
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
        Forwards = new(_rest, Channel, new(config, new RateLimiter(config), clock), config, clock, NullLogger<MaintainerForwards>.Instance);
        var question = new QuestionLink(2, 4);
        Documented = new(ForwardKind.Documentation, new(1, 2, 3), AnswerActionButton.CustomId(AnswerActionButton.DocumentedPrefix, question));
        Requested = new(ForwardKind.FeatureRequest, new(1, 2, 3), AnswerActionButton.RequestCustomId(question, "chronicle"));
    }
}
