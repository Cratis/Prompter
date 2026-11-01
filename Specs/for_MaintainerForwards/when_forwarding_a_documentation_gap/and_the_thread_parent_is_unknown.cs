// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_MaintainerForwards.when_forwarding_a_documentation_gap;

public class and_the_thread_parent_is_unknown : given.a_forward
{
    readonly List<string> _replies = [];

    async Task Because()
    {
        foreach (var parent in (ulong?[])[null, 0])
        {
            _replies.Add(await Forwards.Forward(Documented with { Channel = ThreadChannel(parent) }, TestContext.Current.CancellationToken));
            _replies.Add(await Forwards.Forward(Requested with { Channel = ThreadChannel(parent) }, TestContext.Current.CancellationToken));
        }
    }

    [Fact] void should_reject_both_actions_with_missing_or_zero_parents() => _replies.ShouldEachConformTo(reply => reply == Options.Discord.RateLimitedReply);
    [Fact] void should_not_fetch_the_question() => Discord.Reads.ShouldEqual(0);
    [Fact] void should_not_write_to_discord() => Discord.Writes.ShouldEqual(0);
    [Fact] void should_not_forward() => Channel.DidNotReceiveWithAnyArgs().Post(default, string.Empty, TestContext.Current.CancellationToken);
}
