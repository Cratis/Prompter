// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ConversationContextReader.when_reading_history;

public class and_history_is_unavailable : given.a_history_reader
{
    void Establish() => history.Before(location, Arg.Any<CancellationToken>()).Returns(Task.FromException<IReadOnlyList<HistoryMessage>>(new InvalidOperationException("Deleted or inaccessible")));

    async Task Because() => result = await Read();

    [Fact] void should_keep_the_current_question() => result.Value.ShouldEqual("What about that API?");
    [Fact] void should_have_no_history() => result.Conversation.Messages.ShouldBeEmpty();
    [Fact] void should_mark_context_as_degraded() => result.Conversation.IsDegraded.ShouldBeTrue();
}
