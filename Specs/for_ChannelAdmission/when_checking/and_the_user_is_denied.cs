// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Protection;
using Cratis.Prompter.Specs.Fakes;
using NetCord;
using NetCord.Rest;

namespace Cratis.Prompter.Specs.for_ChannelAdmission.when_checking;

public class and_the_user_is_denied : Specification
{
    readonly FakeDiscordMessages _discord = new();
    RestClient _rest = null!;
    bool _denied;
    bool _other;

    void Because()
    {
        _rest = _discord.Client();
        var options = new PrompterOptions();
        options.Protection.AllowedGuilds = [1];
        options.Protection.DeniedUsers = [3];
        var config = Microsoft.Extensions.Options.Options.Create(options);
        var admission = new Admission(config, new RateLimiter(config), new ControlledTime(DateTimeOffset.UnixEpoch));
        var regular = TextChannel.CreateFromJson(new() { Id = 2, GuildId = 1, Type = ChannelType.TextGuildChannel }, _rest);
        _denied = ChannelAdmission.InScope(admission, new(1, 2, 3), regular);
        _other = ChannelAdmission.InScope(admission, new(1, 2, 4), regular);
    }

    [Fact] void should_refuse_a_denied_user_before_any_action() => _denied.ShouldBeFalse();
    [Fact] void should_keep_other_users_in_scope() => _other.ShouldBeTrue();
}
