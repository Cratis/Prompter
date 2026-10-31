// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_MaintainerForwards.when_forwarding_a_feature_request;

public class and_it_was_already_requested : given.a_forward
{
    async Task Because()
    {
        await Forwards.Forward(Requested, TestContext.Current.CancellationToken);
        Clock.Advance(TimeSpan.FromMinutes(3));
        Reply = await Forwards.Forward(Requested, TestContext.Current.CancellationToken);
    }

    [Fact] void should_refuse_the_repeat() => Reply.ShouldEqual(MaintainerForwards.AlreadyForwardedReply);
    [Fact] void should_post_only_once() => Channel.Received(1).Post(99, Arg.Any<string>(), Arg.Any<CancellationToken>());
}
