// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_ConversationContextReader.when_reading_history;

public class and_the_channel_is_not_public : given.a_history_reader
{
    void Establish() => history.IsPublic(location, Arg.Any<CancellationToken>()).Returns(false);

    async Task Because() => result = await Read();

    [Fact] void should_not_fetch_any_messages() => history.ReceivedCalls().Count().ShouldEqual(1);
    [Fact] void should_keep_current_only() => result.Conversation.Messages.ShouldBeEmpty();
    [Fact] void should_mark_context_as_degraded() => result.Conversation.IsDegraded.ShouldBeTrue();
}
