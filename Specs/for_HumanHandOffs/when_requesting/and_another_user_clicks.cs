// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_HumanHandOffs.when_requesting;

public class and_another_user_clicks : given.a_hand_off
{
    string _reply = null!;

    async Task Because()
    {
        Options.Discord.MaintainerRoleId = 9;
        Discord.Author = 77;
        _reply = await HandOffs.Request(Click, CancellationToken.None);
    }

    [Fact] void should_only_let_the_asker_request() => _reply.ShouldEqual(HumanHandOffs.OnlyAskerReply);
    [Fact] void should_not_post_to_the_channel() => Discord.Writes.ShouldEqual(0);
}
