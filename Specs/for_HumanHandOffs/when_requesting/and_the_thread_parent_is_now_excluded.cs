// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_HumanHandOffs.when_requesting;

public class and_the_thread_parent_is_now_excluded : given.a_hand_off
{
    string _denied = null!;
    string _command = null!;

    async Task Because()
    {
        Options.Discord.MaintainerRoleId = 9;
        Options.Protection.DeniedChannels = [7];
        _denied = await HandOffs.Request(Click with { Channel = ThreadChannel(7) }, TestContext.Current.CancellationToken);
        _command = await HandOffs.Request(Click with { Channel = ThreadChannel(7), CustomId = AnswerActionButton.CustomId(AnswerActionButton.HumanPrefix, new(2, 0)) }, TestContext.Current.CancellationToken);
    }

    [Fact] void should_reject_the_denied_parent_even_if_the_thread_is_in_scope() => _denied.ShouldEqual(Options.Discord.RateLimitedReply);
    [Fact] void should_also_reject_a_slash_command_hand_off() => _command.ShouldEqual(Options.Discord.RateLimitedReply);
    [Fact] void should_not_fetch_the_question() => Discord.Reads.ShouldEqual(0);
    [Fact] void should_not_ping_or_post() => Discord.Writes.ShouldEqual(0);
}
