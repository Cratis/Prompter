// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_ActionButtons.when_authorizing;

public class and_the_interaction_parent_is_denied : given.an_action_interaction
{
    void Establish()
    {
        Options.Protection.DeniedChannels = [7];
        CreateContext(7);
    }

    async Task Because() => await ClickActions();

    [Fact] void should_not_read_or_write_to_discord() => Paths.ShouldBeEmpty();
    [Fact] void should_not_forward_to_maintainers() => Channel.DidNotReceiveWithAnyArgs().Post(default, string.Empty, TestContext.Current.CancellationToken);
    [Fact] void should_not_send_an_acknowledgment() => Payloads.ShouldBeEmpty();
}
