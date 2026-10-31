// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_ConversationContextReader.when_reading_a_selection;

public class and_the_selected_message_is_in_a_thread : given.a_selection_reader
{
    void Establish()
    {
        messages.Add(new(98, 2, now.AddMinutes(-2), "previous", false, false, false));
        messages.Add(new(99, 2, now.AddMinutes(-1), "later", true, true, false));
        messages.Add(new(97, 3, now.AddMinutes(-3), "parent channel", false, false, false));
        messages.Add(new(100, 2, now, "selected duplicate", false, false, false));
        messages.Add(new(101, 2, now.AddMinutes(1), "after selection", false, false, false));
        messages.Add(new(96, 2, now, "same timestamp", false, false, false));
        messages.Add(new(95, 2, now.AddMinutes(-1), "system message", false, false, true));
    }
    async Task Because() => result = await Select();
    [Fact] void should_keep_only_the_preceding_thread_messages() => result.Conversation.Messages.Select(message => message.Content).ShouldEqual(["previous", "later"]);
    [Fact] void should_read_before_the_selected_id() => history.Received(1).Before(location, 20, Arg.Any<CancellationToken>());
}
