// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_ConversationContextReader.when_reading_a_selection;

public class and_context_contains_mentions_and_non_prompter_bots : given.a_selection_reader
{
    void Establish()
    {
        messages.Add(new(97, 2, now.AddMinutes(-3), "third-party bot", true, false, false));
        messages.Add(new(98, 2, now.AddMinutes(-2), "<@123> <@&456> <#789> @everyone @here useful", false, false, false));
        messages.Add(new(99, 2, now.AddMinutes(-1), "Prompter", true, true, false));
    }
    async Task Because() => result = await Select("<@123> @everyone question");
    [Fact] void should_remove_other_bots() => result.Conversation.Messages.Count.ShouldEqual(2);
    [Fact] void should_keep_prompter_as_assistant() => result.Conversation.Messages[^1].Role.ShouldEqual("assistant");
    [Fact] void should_sanitize_history() => result.Conversation.Messages[0].Content.Contains('<').ShouldBeFalse();
    [Fact] void should_remove_mass_mentions() => result.Conversation.Messages[0].Content.Contains('@').ShouldBeFalse();
    [Fact] void should_sanitize_the_selection() => result.Value.ShouldEqual("question");
}
