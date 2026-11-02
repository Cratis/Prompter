// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NetCord;
using NetCord.JsonModels;
using NetCord.Rest;

namespace Cratis.Prompter.Specs.for_MaintainerRole.when_checking;

public class and_the_role_is_unset : Specification
{
    readonly RestClient _client = new();
    bool _unset;
    bool _noRoles;
    bool _matching;
    bool _other;
    bool _noUser;

    void Because()
    {
        var member = Member(10, 20);
        _unset = MaintainerRole.Holds(new() { MaintainerRoleId = null }, member);
        _noRoles = MaintainerRole.Holds(new() { MaintainerRoleId = 10 }, new User(new JsonUser { Id = 1, Username = "plain" }, _client));
        _matching = MaintainerRole.Holds(new() { MaintainerRoleId = 10 }, member);
        _other = MaintainerRole.Holds(new() { MaintainerRoleId = 99 }, member);
        _noUser = MaintainerRole.Holds(new() { MaintainerRoleId = 10 }, null);
    }

    GuildUser Member(params ulong[] roles) =>
        new(new JsonGuildUser { User = new JsonUser { Id = 1, Username = "member" }, RoleIds = roles }, 123, _client);

    void Destroy() => _client.Dispose();

    [Fact] void should_not_exempt_anyone_without_a_configured_role() => _unset.ShouldBeFalse();
    [Fact] void should_fail_closed_for_a_user_without_role_data() => _noRoles.ShouldBeFalse();
    [Fact] void should_recognize_the_configured_role() => _matching.ShouldBeTrue();
    [Fact] void should_not_recognize_a_different_role() => _other.ShouldBeFalse();
    [Fact] void should_fail_closed_without_a_user() => _noUser.ShouldBeFalse();
}
