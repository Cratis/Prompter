// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_NonpaidActions.when_processing;

public class and_refusals_exhaust_global_capacity : given.a_nonpaid_action
{
    int _afterRepeat;
    int _afterGlobalDenial;

    void Establish()
    {
        Options.Protection.ActionClicksPerUser = 0;
        Create();
    }

    async Task Because()
    {
        await Run();
        await Run();
        _afterRepeat = Events.Count;
        for (ulong index = 10; index < 20; index++) await Run(new(1, index, index), channel: ChannelAt(index));
        _afterGlobalDenial = Events.Count;
        for (ulong index = 20; index < 50; index++) await Run(new(1, index, index), channel: ChannelAt(index));
    }

    [Fact] void should_suppress_a_repeated_refusal_without_callbacks() => _afterRepeat.ShouldEqual(1);
    [Fact] void should_charge_even_a_suppressed_refusal_to_the_global_cap() => _afterGlobalDenial.ShouldEqual(11);
    [Fact] void should_make_no_more_rest_calls_after_global_exhaustion() => Events.Count.ShouldEqual(11);
    [Fact] void should_never_defer_or_fetch_messages_for_rejected_users() => Events.All(item => item == "refuse").ShouldBeTrue();
}
