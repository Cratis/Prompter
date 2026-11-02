// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_AnsweringCommand.when_pausing;

public class and_the_caller_is_not_authorized : given.an_answering_command
{
    bool _unset;
    bool _noRole;
    bool _wrongGuild;

    async Task Because()
    {
        Options.Discord.MaintainerRoleId = null;
        Invoke(Guild, Role);
        await Command.Pause();
        _unset = Replied("Only maintainers can do that.");
        Payloads.Clear();

        Options.Discord.MaintainerRoleId = Role;
        Invoke(Guild, 99);
        await Command.Pause();
        _noRole = Replied("Only maintainers can do that.");
        Payloads.Clear();

        Invoke(2, Role);
        await Command.Pause();
        _wrongGuild = Replied("Only maintainers can do that.");
    }

    [Fact] void should_refuse_when_no_maintainer_role_is_configured() => _unset.ShouldBeTrue();
    [Fact] void should_refuse_a_user_without_the_role() => _noRole.ShouldBeTrue();
    [Fact] void should_refuse_a_guild_that_is_not_allowed() => _wrongGuild.ShouldBeTrue();
    [Fact] void should_change_nothing() => (Switch.Paused || Pauses.ReceivedCalls().Any()).ShouldBeFalse();
}
