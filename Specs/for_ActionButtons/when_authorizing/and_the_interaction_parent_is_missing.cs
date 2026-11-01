// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NetCord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ActionButtons.when_authorizing;

public class and_the_interaction_parent_is_missing : given.an_action_interaction
{
    void Establish() => CreateContext(parent: null);

    async Task Because()
    {
        await ClickActions();
        ForgetGuild();
        await ClickActions();
    }

    [Fact] void should_exercise_a_thread_with_the_sdk_zero_parent() => ((GuildThread)ButtonContext.Channel).ParentId.ShouldEqual(0UL);
    [Fact] void should_not_read_or_write_to_discord() => Paths.ShouldBeEmpty();
    [Fact] void should_not_forward_to_maintainers() => Channel.DidNotReceiveWithAnyArgs().Post(default, string.Empty, TestContext.Current.CancellationToken);
    [Fact] void should_not_send_an_acknowledgment() => Payloads.ShouldBeEmpty();
}
