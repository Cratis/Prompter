// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_ConversationContextReader.when_reading_a_selection;

public class and_there_are_more_than_twenty_messages : given.a_selection_reader
{
    void Establish()
    {
        for (ulong id = 1; id < 100; id++) messages.Add(new(id, 2, now.AddMinutes(-1), $"text {id}", false, false, false));
    }
    async Task Because() => result = await Select();
    [Fact] void should_keep_twenty() => result.Conversation.Messages.Count.ShouldEqual(20);
    [Fact] void should_keep_the_newest_in_chronological_order() => result.Conversation.Messages.Select(message => message.Content).ShouldEqual(Enumerable.Range(80, 20).Select(id => $"text {id}"));
    [Fact] void should_not_change_the_legacy_window() => ConversationWindow.Recent.ShouldEqual(new(10, 6000, TimeSpan.FromMinutes(15), false));
}
