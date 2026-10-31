// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_ConversationContextReader.when_reading_history;

public class and_the_replied_message_is_in_the_window : given.a_history_reader
{
    void Establish() => history.Before(location, Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<HistoryMessage>>(
    [
        Human(30, "first"),
        Prompter(40, "Which version are you on?", Now.AddMinutes(-2)),
        Human(50, "later")
    ]));

    async Task Because() => result = await ReadReplyingTo(Prompter(40, "Which version are you on?", Now.AddMinutes(-2)));

    [Fact] void should_include_the_replied_message_once_in_order() => result.Conversation.Messages.Select(message => message.Content).ShouldEqual(["first", "Which version are you on?", "later"]);
}
