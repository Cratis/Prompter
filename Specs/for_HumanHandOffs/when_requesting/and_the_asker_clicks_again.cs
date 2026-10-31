// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_HumanHandOffs.when_requesting;

public class and_the_asker_clicks_again : given.a_hand_off
{
    string _first = null!;
    string _second = null!;

    async Task Because()
    {
        Options.Discord.MaintainerRoleId = 9;
        _first = await HandOffs.Request(Click, CancellationToken.None);
        _second = await HandOffs.Request(Click with { CustomId = AnswerActionButton.CustomId(AnswerActionButton.HumanPrefix, new(2, 6)) }, CancellationToken.None);
    }

    [Fact] void should_accept_the_first_click() => _first.ShouldEqual(HumanHandOffs.RequestedReply);
    [Fact] void should_refuse_a_second_hand_off_in_the_conversation() => _second.ShouldEqual(HumanHandOffs.AlreadyRequestedReply);
    [Fact] void should_post_only_once() => Discord.Writes.ShouldEqual(1);
}
