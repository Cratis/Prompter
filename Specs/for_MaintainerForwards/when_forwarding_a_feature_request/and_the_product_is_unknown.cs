// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_MaintainerForwards.when_forwarding_a_feature_request;

public class and_the_product_is_unknown : given.a_forward
{
    string _content = null!;

    async Task Because()
    {
        Channel.Post(Arg.Any<ulong>(), Arg.Do<string>(content => _content = content), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        Reply = await Forwards.Forward(Requested with { CustomId = AnswerActionButton.RequestCustomId(new(2, 4), "unknown") }, TestContext.Current.CancellationToken);
    }

    [Fact] void should_say_the_product_is_unknown() => _content.ShouldContain("Feature request (product unknown)");
}
