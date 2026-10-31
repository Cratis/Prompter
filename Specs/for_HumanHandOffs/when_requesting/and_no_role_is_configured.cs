// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_HumanHandOffs.when_requesting;

public class and_no_role_is_configured : given.a_hand_off
{
    string _reply = null!;

    async Task Because() => _reply = await HandOffs.Request(Click, CancellationToken.None);

    [Fact] void should_not_be_configured() => _reply.ShouldEqual(HumanHandOffs.NotConfiguredReply);
    [Fact] void should_not_post_to_the_channel() => Discord.Writes.ShouldEqual(0);
}
