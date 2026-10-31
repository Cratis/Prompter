// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_ConversationContextReader.when_reading_a_selection;

public class and_the_character_budget_is_exhausted : given.a_selection_reader
{
    void Establish()
    {
        messages.Add(new(98, 2, now.AddMinutes(-2), new string('a', 4000), false, false, false));
        messages.Add(new(99, 2, now.AddMinutes(-1), new string('b', 4000), false, false, false));
    }
    async Task Because() => result = await Select();
    [Fact] void should_bound_history_to_six_thousand_characters() => result.Conversation.Messages.Sum(message => message.Content.Length).ShouldEqual(6000);
    [Fact] void should_keep_the_current_text_separate() => result.Value.ShouldEqual("selected question");
    [Fact] void should_prefer_the_newest_text() => result.Conversation.Messages[^1].Content.Length.ShouldEqual(4000);
}
