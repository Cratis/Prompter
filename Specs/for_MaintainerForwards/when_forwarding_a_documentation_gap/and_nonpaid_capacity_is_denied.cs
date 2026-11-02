// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_MaintainerForwards.when_forwarding_a_documentation_gap;

public class and_nonpaid_capacity_is_denied : given.a_forward
{
    void Establish() => Options.Protection.ActionClicksPerUser = 0;
    async Task Because() => await Run(Documented);

    [Fact] void should_deny_before_fetching_the_question() => Discord.Reads.ShouldEqual(0);
    [Fact] void should_not_post_to_discord() => Channel.ReceivedCalls().Count().ShouldEqual(0);
    [Fact] void should_use_the_existing_rate_limited_reply() => Reply.ShouldEqual(Options.Discord.RateLimitedReply);
}
