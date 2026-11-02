// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_NonpaidActions.when_entering;

public class and_channel_capacity_is_exhausted : given.a_nonpaid_action
{
    string[] _rejected = [];
    int _afterRefill;

    void Establish()
    {
        Options.Protection.ActionChannelPerMinute = 1;
        Create();
    }

    async Task Because()
    {
        await Run();
        Events.Clear();
        await Run(Actor with { UserId = 4 });
        _rejected = [.. Events];
        Events.Clear();
        Clock.Advance(TimeSpan.FromMinutes(1));
        await Run(Actor with { UserId = 5 });
        _afterRefill = Events.Count;
    }

    [Fact] void should_share_channel_quota_across_users() => _rejected.ShouldEqual(["refuse"]);
    [Fact] void should_not_defer_or_lookup_a_denied_action() => _rejected.Length.ShouldEqual(1);
    [Fact] void should_refill_the_channel_window_at_sixty_seconds() => _afterRefill.ShouldEqual(3);
}
