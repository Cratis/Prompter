// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ConversationContextReader.when_reading_history;

public class and_messages_are_out_of_scope : given.a_history_reader
{
    void Establish() => history.Before(location, Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<HistoryMessage>>(
    [
        Human(90, "later"),
        Human(10, "earlier") with { CreatedAt = Now.AddMinutes(-15) },
        Human(9, "expired") with { CreatedAt = Now.AddMinutes(-15).AddTicks(-1) },
        Human(100, "trigger"),
        Human(101, "future") with { CreatedAt = Now.AddMinutes(1) },
        Human(80, "sibling") with { ChannelId = 3 },
        Human(70, "other bot") with { IsBot = true },
        Human(60, "system") with { IsSystem = true },
        Human(50, "own answer") with { IsBot = true, IsSelf = true }
    ]));

    async Task Because() => result = await Read();

    [Fact] void should_keep_only_recent_earlier_messages_in_order() => result.Conversation.Messages.Select(message => message.Content).ShouldEqual(["earlier", "own answer", "later"]);
    [Fact] void should_label_only_own_answers_as_assistant() => result.Conversation.Messages.Select(message => message.Role).ShouldEqual(["user", "assistant", "user"]);
    [Fact] void should_keep_the_current_question() => result.Value.ShouldEqual("What about that API?");
    [Fact] void should_not_mark_success_as_degraded() => result.Conversation.IsDegraded.ShouldBeFalse();
}
