// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_ConversationContextReader.when_reading_history;

public class and_the_replied_message_is_in_another_channel : given.a_history_reader
{
    async Task Because() => result = await ReadReplyingTo(Prompter(40, "Which version are you on?", Now.AddMinutes(-40)) with { ChannelId = 3 });

    [Fact] void should_ignore_the_replied_message() => result.Conversation.Messages.ShouldBeEmpty();
}
