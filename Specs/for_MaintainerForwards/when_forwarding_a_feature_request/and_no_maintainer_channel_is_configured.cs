// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_MaintainerForwards.when_forwarding_a_feature_request;

public class and_no_maintainer_channel_is_configured : given.a_forward
{
    void Establish() => Options.Discord.MaintainerChannelId = null;

    async Task Because() => Reply = await Forwards.Forward(Requested, TestContext.Current.CancellationToken);

    [Fact] void should_say_it_is_not_configured() => Reply.ShouldEqual(MaintainerForwards.NotConfiguredReply);
    [Fact] void should_not_post() => Channel.DidNotReceive().Post(Arg.Any<ulong>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
}
