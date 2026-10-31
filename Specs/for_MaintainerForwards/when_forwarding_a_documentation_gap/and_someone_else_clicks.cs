// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_MaintainerForwards.when_forwarding_a_documentation_gap;

public class and_someone_else_clicks : given.a_forward
{
    async Task Because() => Reply = await Forwards.Forward(Documented with { Actor = new(1, 2, 8) }, TestContext.Current.CancellationToken);

    [Fact] void should_only_allow_the_asker() => Reply.ShouldEqual(MaintainerForwards.OnlyAskerReply);
    [Fact] void should_not_post() => Channel.DidNotReceive().Post(Arg.Any<ulong>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
}
