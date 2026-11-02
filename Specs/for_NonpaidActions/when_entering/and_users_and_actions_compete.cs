// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_NonpaidActions.when_entering;

public class and_users_and_actions_compete : given.a_nonpaid_action
{
    string[] _order = [];
    int _afterGlobalDenial;
    int _afterScopeDenial;

    void Establish()
    {
        Options.Protection.ActionPerMinute = 2;
        Create();
    }

    async Task Because()
    {
        await Run();
        _order = [.. Events];
        await Run(Actor with { UserId = 4 });
        _afterGlobalDenial = Events.Count;
        await Run(Actor with { UserId = 5 });
        _afterGlobalDenial = Events.Count - _afterGlobalDenial;
        Clock.Advance(TimeSpan.FromMinutes(1));
        var count = Events.Count;
        await Run(Actor with { GuildId = null });
        _afterScopeDenial = Events.Count - count;
        await Run(Actor with { UserId = 6 });
    }

    [Fact] void should_defer_before_lookup_and_followup() => _order.ShouldEqual(["defer", "work", "followup"]);
    [Fact] void should_silently_deny_global_exhaustion() => _afterGlobalDenial.ShouldEqual(0);
    [Fact] void should_keep_unknown_scope_silent() => _afterScopeDenial.ShouldEqual(0);
    [Fact] void should_refill_global_throughput_at_sixty_seconds() => Events.Count.ShouldEqual(9);
}
