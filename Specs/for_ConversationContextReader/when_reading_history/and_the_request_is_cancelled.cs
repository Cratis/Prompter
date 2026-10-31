// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_ConversationContextReader.when_reading_history;

public class and_the_request_is_cancelled : given.a_history_reader
{
    void Establish() => cancellation.Cancel();

    async Task Because() => result = await Read();

    [Fact] void should_keep_current_only() => result.Value.ShouldEqual("What about that API?");
    [Fact] void should_not_fetch_history() => history.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_mark_context_as_degraded() => result.Conversation.IsDegraded.ShouldBeTrue();
}
