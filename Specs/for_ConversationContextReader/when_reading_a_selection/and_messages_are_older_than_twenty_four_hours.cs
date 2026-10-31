// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_ConversationContextReader.when_reading_a_selection;

public class and_messages_are_older_than_twenty_four_hours : given.a_selection_reader
{
    void Establish()
    {
        location = location with { CreatedAt = now.AddDays(-7) };
        messages.Add(new(97, 2, location.CreatedAt.AddHours(-24).AddTicks(-1), "too old", false, false, false));
        messages.Add(new(98, 2, location.CreatedAt.AddHours(-24), "inclusive boundary", false, false, false));
        messages.Add(new(99, 2, location.CreatedAt.AddHours(-1), "near selection", false, false, false));
    }
    async Task Because() => result = await Select();
    [Fact] void should_measure_age_from_the_selection_not_now() => result.Conversation.Messages.Select(message => message.Content).ShouldEqual(["inclusive boundary", "near selection"]);
}
