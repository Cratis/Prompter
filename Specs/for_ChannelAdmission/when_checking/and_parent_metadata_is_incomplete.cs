// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Specs.Fakes;
using NetCord;
using NetCord.Rest;

namespace Cratis.Prompter.Specs.for_ChannelAdmission.when_checking;

public class and_parent_metadata_is_incomplete : Specification
{
    readonly FakeDiscordMessages _discord = new();
    readonly List<AdmissionDecision> _unknown = [];
    RestClient _rest = null!;
    bool _regular;
    bool _knownThread;
    bool _denied;
    bool _missingChannel;
    bool _mismatchedChannel;
    bool _unknownChannel;
    bool _unknownBeforeRegular;

    void Because()
    {
        _rest = _discord.Client();
        var options = new PrompterOptions();
        options.Protection.AllowedGuilds = [1];
        var config = Microsoft.Extensions.Options.Options.Create(options);
        var admission = new Admission(config, new RateLimiter(config), new ControlledTime(DateTimeOffset.UnixEpoch));
        var actor = new DiscordActor(1, 2, 3);
        ChannelType[] types = [ChannelType.PublicGuildThread, ChannelType.PrivateGuildThread, ChannelType.AnnouncementGuildThread];
        ulong?[] parents = [null, 0];
        foreach (var type in types)
        {
            foreach (var parent in parents)
            {
                var thread = TextChannel.CreateFromJson(new() { Id = 2, GuildId = 1, Type = type, ParentId = parent }, _rest);
                _unknown.Add(ChannelAdmission.TryEnter(admission, actor, thread, "unknown parent"));
            }
        }
        var regular = TextChannel.CreateFromJson(new() { Id = 2, GuildId = 1, Type = ChannelType.TextGuildChannel }, _rest);
        var known = TextChannel.CreateFromJson(new() { Id = 2, GuildId = 1, Type = ChannelType.PublicGuildThread, ParentId = 7 }, _rest);
        _regular = ChannelAdmission.InScope(admission, actor, regular);
        _knownThread = ChannelAdmission.InScope(admission, actor, known);
        _missingChannel = ChannelAdmission.InScope(admission, actor, null);
        _unknownChannel = ChannelAdmission.InScope(admission, actor, TextChannel.CreateFromJson(new() { Id = 2, GuildId = 1, Type = (ChannelType)int.MaxValue }, _rest));
        _mismatchedChannel = ChannelAdmission.InScope(admission, actor with { ChannelId = 9 }, regular);
        _unknownBeforeRegular = ChannelAdmission.TryEnter(admission, actor, regular, "valid request").Allowed;
        options.Protection.DeniedChannels = [7];
        _denied = ChannelAdmission.InScope(admission, actor, known);
    }

    [Fact] void should_reject_all_thread_kinds_without_a_known_parent() => _unknown.Count(decision => !decision.Allowed && !decision.Reply).ShouldEqual(6);
    [Fact] void should_distinguish_a_regular_channel_from_an_unknown_thread() => _regular.ShouldBeTrue();
    [Fact] void should_allow_a_known_parent() => _knownThread.ShouldBeTrue();
    [Fact] void should_reject_a_denied_parent() => _denied.ShouldBeFalse();
    [Fact] void should_reject_missing_channel_context() => _missingChannel.ShouldBeFalse();
    [Fact] void should_reject_an_unknown_channel_kind() => _unknownChannel.ShouldBeFalse();
    [Fact] void should_bind_context_to_the_actor_channel() => _mismatchedChannel.ShouldBeFalse();
    [Fact] void should_not_consume_admission_for_unknown_parents() => _unknownBeforeRegular.ShouldBeTrue();
    [Fact] void should_not_fetch_or_post() => (_discord.Reads + _discord.Writes).ShouldEqual(0);

    void Destroy() => _rest.Dispose();
}
