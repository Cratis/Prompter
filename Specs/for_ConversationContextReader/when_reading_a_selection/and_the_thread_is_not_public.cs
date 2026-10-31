// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ConversationContextReader.when_reading_a_selection;

public class and_the_thread_is_not_public : given.a_selection_reader
{
    void Establish() => history.IsPublic(Arg.Any<ConversationLocation>(), Arg.Any<CancellationToken>()).Returns(false);
    async Task Because() => result = await Select();
    [Fact] void should_keep_the_selection() => result.Value.ShouldEqual("selected question");
    [Fact] void should_mark_context_as_degraded() => result.Conversation.IsDegraded.ShouldBeTrue();
    [Fact] void should_not_fetch_thread_messages() => history.DidNotReceive().Before(Arg.Any<ConversationLocation>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
}
