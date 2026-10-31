// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_MaintainerForwards.when_forwarding_a_feature_request;

public class and_the_asker_requests_it : given.a_forward
{
    string _content = null!;

    async Task Because()
    {
        Channel.Post(Arg.Any<ulong>(), Arg.Do<string>(content => _content = content), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        Reply = await Forwards.Forward(Requested, TestContext.Current.CancellationToken);
    }

    [Fact] void should_confirm() => Reply.ShouldEqual(MaintainerForwards.ForwardedReply);
    [Fact] void should_post_once_to_the_maintainer_channel() => Channel.Received(1).Post(99, Arg.Any<string>(), Arg.Any<CancellationToken>());
    [Fact] void should_label_the_request_with_the_product() => _content.ShouldContain("Feature request (chronicle)");
    [Fact] void should_include_the_jump_link() => _content.ShouldContain("https://discord.com/channels/1/2/4");
}
