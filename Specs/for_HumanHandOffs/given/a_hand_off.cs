// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Specs.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NetCord.Rest;

namespace Cratis.Prompter.Specs.for_HumanHandOffs.given;

public class a_hand_off : Specification
{
    RestClient _rest = null!;

    protected FakeDiscordMessages Discord { get; private set; } = null!;

    protected PrompterOptions Options { get; private set; } = null!;
    protected HumanHandOffs HandOffs { get; private set; } = null!;
    protected HandOffClick Click { get; set; } = null!;
    void Destroy() => _rest.Dispose();

    void Establish()
    {
        var clock = new ControlledTime(new(2026, 6, 5, 12, 0, 0, TimeSpan.Zero));
        Options = new();
        Options.Protection.AllowedGuilds = [1];
        Options.Discord.AskChannelId = 2;
        Options.Discord.HelpForumChannelId = 5;
        var config = Microsoft.Extensions.Options.Options.Create(Options);
        Discord = new() { Author = 3 };
        _rest = Discord.Client();
        HandOffs = new(
            _rest,
            new(config, new RateLimiter(config), clock),
            new(config, clock),
            config,
            clock,
            NullLogger<HumanHandOffs>.Instance);
        Click = new(new(1, 2, 3), AnswerActionButton.CustomId(AnswerActionButton.HumanPrefix, new(2, 4)), 4, 5, null);
    }
}
