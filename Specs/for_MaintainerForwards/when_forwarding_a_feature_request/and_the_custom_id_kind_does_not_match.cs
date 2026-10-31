// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_MaintainerForwards.when_forwarding_a_feature_request;

public class and_the_custom_id_kind_does_not_match : given.a_forward
{
    async Task Because() => Reply = await Forwards.Forward(Requested with { CustomId = Documented.CustomId }, TestContext.Current.CancellationToken);

    [Fact] void should_be_unavailable() => Reply.ShouldEqual(MaintainerForwards.UnavailableReply);
    [Fact] void should_not_post() => Channel.DidNotReceive().Post(Arg.Any<ulong>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
}
