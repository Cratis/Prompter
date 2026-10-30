// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_ConversationContextReader.when_reading_history;

public class and_spend_has_not_been_admitted : given.a_history_reader
{
    async Task Because() => result = await reader.For("current question", location);

    [Fact] void should_not_call_discord() => history.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_keep_current_only() => result.Value.ShouldEqual("current question");
    [Fact] void should_mark_context_as_degraded() => result.Conversation.IsDegraded.ShouldBeTrue();
}
