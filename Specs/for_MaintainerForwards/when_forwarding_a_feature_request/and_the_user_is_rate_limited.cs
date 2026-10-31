// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_MaintainerForwards.when_forwarding_a_feature_request;

public class and_the_user_is_rate_limited : given.a_forward
{
    void Establish()
    {
        Options.Discord.RateLimit.MaxQuestions = 1;
    }

    async Task Because()
    {
        await Forwards.Forward(Requested with { CustomId = AnswerActionButton.RequestCustomId(new(2, 5), "chronicle") }, TestContext.Current.CancellationToken);
        Channel.ClearReceivedCalls();
        Reply = await Forwards.Forward(Requested, TestContext.Current.CancellationToken);
    }

    [Fact] void should_refuse() => Reply.ShouldEqual(Options.Discord.RateLimitedReply);
    [Fact] void should_not_post() => Channel.DidNotReceive().Post(Arg.Any<ulong>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
}
