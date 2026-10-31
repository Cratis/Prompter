// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_MaintainerForwards.when_forwarding_a_documentation_gap;

public class and_the_asker_forwards_a_refusal : given.a_forward
{
    string _content = null!;

    async Task Because()
    {
        Channel.Post(Arg.Any<ulong>(), Arg.Do<string>(content => _content = content), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        Reply = await Forwards.Forward(Documented, TestContext.Current.CancellationToken);
    }

    [Fact] void should_confirm() => Reply.ShouldEqual(MaintainerForwards.ForwardedReply);
    [Fact] void should_post_once_to_the_maintainer_channel() => Channel.Received(1).Post(99, Arg.Any<string>(), Arg.Any<CancellationToken>());
    [Fact] void should_include_the_question() => _content.ShouldContain("How do I");
    [Fact] void should_strip_the_mention() => _content.ShouldNotContain("<@123>");
    [Fact] void should_include_the_jump_link() => _content.ShouldContain("https://discord.com/channels/1/2/4");
}
