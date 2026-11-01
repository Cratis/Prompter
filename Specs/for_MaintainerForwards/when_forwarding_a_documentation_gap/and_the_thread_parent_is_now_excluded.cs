// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_MaintainerForwards.when_forwarding_a_documentation_gap;

public class and_the_thread_parent_is_now_excluded : given.a_forward
{
    string _deniedDocumentation = null!;
    string _deniedRequest = null!;

    async Task Because()
    {
        Options.Protection.DeniedChannels = [7];
        _deniedDocumentation = await Forwards.Forward(Documented with { Channel = ThreadChannel(7) }, TestContext.Current.CancellationToken);
        _deniedRequest = await Forwards.Forward(Requested with { Channel = ThreadChannel(7) }, TestContext.Current.CancellationToken);
    }

    [Fact] void should_reject_documentation_forwards_from_the_denied_parent() => _deniedDocumentation.ShouldEqual(Options.Discord.RateLimitedReply);
    [Fact] void should_reject_feature_forwards_from_the_denied_parent() => _deniedRequest.ShouldEqual(Options.Discord.RateLimitedReply);
    [Fact] void should_not_fetch_the_question() => Discord.Reads.ShouldEqual(0);
    [Fact] void should_not_make_discord_writes() => Discord.Writes.ShouldEqual(0);
    [Fact] void should_not_post_to_the_maintainer_channel() => Channel.DidNotReceiveWithAnyArgs().Post(default, string.Empty, TestContext.Current.CancellationToken);
}
