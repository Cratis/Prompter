// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_NonpaidActions.when_entering;

public class and_a_turn_is_carried_across_the_acknowledgement : given.a_nonpaid_action
{
    int _firstRefusal;
    int _suppressedRefusal;
    int _afterRefill;

    void Establish()
    {
        Options.Protection.ActionClicksPerUser = 1;
        Create();
    }

    async Task Because()
    {
        await Run();
        Events.Clear();
        await Run();
        _firstRefusal = Events.Count;
        Events.Clear();
        await Run();
        _suppressedRefusal = Events.Count;
        Clock.Advance(TimeSpan.FromSeconds(Options.Protection.ActionWindowSeconds));
        await Run();
        _afterRefill = Events.Count;
    }

    [Fact] void should_only_send_an_immediate_reply_for_quota_denial() => _firstRefusal.ShouldEqual(1);
    [Fact] void should_invoke_no_callbacks_after_the_refusal_is_suppressed() => _suppressedRefusal.ShouldEqual(0);
    [Fact] void should_refill_the_user_window_independently() => _afterRefill.ShouldEqual(3);
}
