// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Specs.Fakes;
using NetCord;
using NetCord.Rest;

namespace Cratis.Prompter.Specs.for_NonpaidActions.given;

public class a_nonpaid_action : Specification
{
    readonly FakeDiscordMessages _discord = new();
    RestClient _rest = null!;

    protected PrompterOptions Options { get; private set; } = null!;
    protected ManualClock Clock { get; private set; } = null!;
    protected Admission Admission { get; private set; } = null!;
    protected NonpaidActions Actions { get; private set; } = null!;
    protected TextChannel Channel { get; private set; } = null!;
    protected ICollection<string> Events { get; } = [];
    protected Func<CancellationToken, Task<string>> Work { get; set; } = null!;
    protected DiscordActor Actor { get; } = new(1, 2, 3);

    protected void Create()
    {
        var config = Microsoft.Extensions.Options.Options.Create(Options);
        Admission = new(config, new RateLimiter(config), Clock);
        Actions = new(Admission, config, Clock);
    }

    protected TextChannel ChannelAt(ulong id) => TextChannel.CreateFromJson(new() { Id = id, GuildId = 1, Type = ChannelType.TextGuildChannel }, _rest);

    protected Task Run(DiscordActor? actor = null, TextChannel? channel = null, CancellationToken cancellationToken = default) =>
        Actions.Run(actor ?? Actor, channel ?? Channel, new(Defer, Work, Refuse, Followup), cancellationToken);

    Task Defer(CancellationToken token)
    {
        Events.Add("defer");
        return Task.CompletedTask;
    }

    Task Refuse(string reply, CancellationToken token)
    {
        reply.ShouldEqual(Options.Discord.RateLimitedReply);
        Events.Add("refuse");
        return Task.CompletedTask;
    }

    Task Followup(string reply, CancellationToken token)
    {
        Events.Add("followup");
        return Task.CompletedTask;
    }

    Task<string> Perform(CancellationToken token)
    {
        Events.Add("work");
        return Task.FromResult("done");
    }

    void Establish()
    {
        Options = new();
        Options.Protection.AllowedGuilds = [1];
        Clock = new(new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        _rest = _discord.Client();
        Channel = TextChannel.CreateFromJson(new() { Id = 2, GuildId = 1, Type = ChannelType.TextGuildChannel }, _rest);
        Work = Perform;
    }

    void Destroy()
    {
        _rest.Dispose();
        _discord.Dispose();
    }
}
