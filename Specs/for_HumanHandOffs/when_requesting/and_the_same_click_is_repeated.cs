// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_HumanHandOffs.when_requesting;

public class and_the_same_click_is_repeated : given.a_hand_off
{
    string _second = null!;

    async Task Because()
    {
        Options.Discord.MaintainerRoleId = 9;
        await HandOffs.Request(Click, CancellationToken.None);
        _second = await HandOffs.Request(Click, CancellationToken.None);
    }

    [Fact] void should_be_rate_limited() => _second.ShouldEqual(Options.Discord.RateLimitedReply);
    [Fact] void should_post_only_once() => Discord.Writes.ShouldEqual(1);
}
