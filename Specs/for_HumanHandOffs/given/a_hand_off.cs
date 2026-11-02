// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Specs.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NetCord;
using NetCord.Rest;

namespace Cratis.Prompter.Specs.for_HumanHandOffs.given;

public class a_hand_off : Specification
{
    RestClient _rest = null!;

    protected FakeDiscordMessages Discord { get; private set; } = null!;

    protected PrompterOptions Options { get; private set; } = null!;
    protected Admission Admission { get; private set; } = null!;
    protected ControlledTime Clock { get; private set; } = null!;
    protected HumanHandOffs HandOffs { get; private set; } = null!;
    protected NonpaidActions Actions { get; private set; } = null!;
    protected HandOffClick Click { get; set; } = null!;
    protected TextChannel RegularChannel(ulong id) => TextChannel.CreateFromJson(new() { Id = id, GuildId = 1, Type = ChannelType.TextGuildChannel }, _rest);

    protected TextChannel ThreadChannel(ulong? parent) => TextChannel.CreateFromJson(new() { Id = 2, GuildId = 1, Type = ChannelType.PublicGuildThread, ParentId = parent }, _rest);

    protected async Task<string> Run(HandOffClick click)
    {
        var reply = string.Empty;
        Task Capture(string text, CancellationToken token)
        {
            reply = text;
            return Task.CompletedTask;
        }
        await Actions.Run(click.Actor, click.Channel, new(_ => Task.CompletedTask, token => HandOffs.Request(click, token), Capture, Capture), TestContext.Current.CancellationToken);
        return reply;
    }

    void Destroy() => _rest.Dispose();

    void Establish()
    {
        Clock = new(new(2026, 6, 5, 12, 0, 0, TimeSpan.Zero));
        Options = new();
        Options.Protection.AllowedGuilds = [1];
        Options.Discord.AskChannelId = 2;
        Options.Discord.HelpForumChannelId = 5;
        var config = Microsoft.Extensions.Options.Options.Create(Options);
        Discord = new() { Author = 3 };
        _rest = Discord.Client();
        Admission = new(config, new RateLimiter(config), Clock);
        HandOffs = new(
            _rest,
            Admission,
            new(config, Clock),
            config,
            Clock,
            NullLogger<HumanHandOffs>.Instance);
        Actions = new(Admission, config, Clock);
        Click = new(new(1, 2, 3), AnswerActionButton.CustomId(AnswerActionButton.HumanPrefix, new(2, 4)), 4, 5, null)
        {
            Channel = RegularChannel(2)
        };
    }
}
