// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ConversationContextReader.when_reading_a_selection;

public class and_the_thread_cannot_be_read : given.a_selection_reader
{
    void Establish() => history.Before(Arg.Any<ConversationLocation>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<IReadOnlyList<HistoryMessage>>(new InvalidOperationException("Deleted or inaccessible")));
    async Task Because() => result = await Select();
    [Fact] void should_fall_back_to_the_selection() => result.Value.ShouldEqual("selected question");
    [Fact] void should_have_no_history() => result.Conversation.Messages.Count.ShouldEqual(0);
    [Fact] void should_mark_context_as_degraded() => result.Conversation.IsDegraded.ShouldBeTrue();
}
