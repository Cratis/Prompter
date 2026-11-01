// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_HumanHandOffs.when_requesting;

public class and_the_thread_parent_is_unknown : given.a_hand_off
{
    readonly List<string> _replies = [];

    async Task Because()
    {
        Options.Discord.MaintainerRoleId = 9;
        _replies.Add(await HandOffs.Request(Click with { Channel = ThreadChannel(null) }, TestContext.Current.CancellationToken));
        _replies.Add(await HandOffs.Request(Click with { Channel = ThreadChannel(0) }, TestContext.Current.CancellationToken));
    }

    [Fact] void should_reject_missing_and_zero_parent_metadata() => _replies.ShouldEachConformTo(reply => reply == Options.Discord.RateLimitedReply);
    [Fact] void should_not_fetch_the_question() => Discord.Reads.ShouldEqual(0);
    [Fact] void should_not_ping_or_post() => Discord.Writes.ShouldEqual(0);
}
