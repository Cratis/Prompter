// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_ConversationContextReader.when_reading_a_selection;

public class and_the_selected_message_is_in_a_channel : given.a_selection_reader
{
    void Establish() => in_thread = false;
    async Task Because() => result = await Select();
    [Fact] void should_use_only_the_selection() => result.Conversation.Messages.Count.ShouldEqual(0);
    [Fact] void should_not_read_history() => history.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_keep_the_current_text() => result.Value.ShouldEqual("selected question");
}
