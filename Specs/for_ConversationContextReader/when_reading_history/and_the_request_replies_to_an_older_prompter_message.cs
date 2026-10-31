// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_ConversationContextReader.when_reading_history;

public class and_the_request_replies_to_an_older_prompter_message : given.a_history_reader
{
    async Task Because() => result = await ReadReplyingTo(Prompter(40, "Which version are you on?", Now.AddMinutes(-40)));

    [Fact] void should_include_the_replied_message() => result.Conversation.Messages.Select(message => (message.Role, message.Content)).ShouldEqual([("assistant", "Which version are you on?")]);
    [Fact] void should_keep_the_current_question() => result.Value.ShouldEqual("Version 2.");
    [Fact] void should_not_mark_success_as_degraded() => result.Conversation.IsDegraded.ShouldBeFalse();
}
