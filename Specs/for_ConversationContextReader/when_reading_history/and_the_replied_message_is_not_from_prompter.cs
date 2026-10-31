// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_ConversationContextReader.when_reading_history;

public class and_the_replied_message_is_not_from_prompter : given.a_history_reader
{
    async Task Because() => result = await ReadReplyingTo(Prompter(40, "Someone else's message", Now.AddMinutes(-40)) with { IsSelf = false });

    [Fact] void should_ignore_the_replied_message() => result.Conversation.Messages.ShouldBeEmpty();
}
