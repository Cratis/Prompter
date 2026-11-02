// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_HumanHandOffs.when_requesting;

public class and_nonpaid_capacity_is_denied : given.a_hand_off
{
    string _reply = string.Empty;
    void Establish()
    {
        Options.Discord.MaintainerRoleId = 9;
        Options.Protection.ActionClicksPerUser = 0;
    }

    async Task Because() => _reply = await Run(Click);

    [Fact] void should_deny_before_fetching_the_question() => Discord.Reads.ShouldEqual(0);
    [Fact] void should_not_create_a_thread_or_post() => Discord.Writes.ShouldEqual(0);
    [Fact] void should_use_the_existing_rate_limited_reply() => _reply.ShouldEqual(Options.Discord.RateLimitedReply);
}
