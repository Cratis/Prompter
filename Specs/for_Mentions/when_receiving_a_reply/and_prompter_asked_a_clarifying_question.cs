// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Mentions.when_receiving_a_reply;

public class and_prompter_asked_a_clarifying_question : given.an_admitted_message
{
    void Establish()
    {
        History.IsPublic(Arg.Any<ConversationLocation>(), Arg.Any<CancellationToken>()).Returns(true);
        History.Before(Arg.Any<ConversationLocation>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<HistoryMessage>>([]));
        Message = Build(100, 5, 3, "version 2", referenced: Json(50, 5, 42, "Which version are you on?", authorIsBot: true));
    }

    async Task Because() => await Mentions.HandleAsync(Message);

    [Fact] void should_keep_the_reply_as_the_question() => Asked().Value.ShouldEqual("version 2");
    [Fact] void should_include_prompters_question_as_assistant_context() => Conversation().Select(message => (message.Role, message.Content)).ShouldEqual([("assistant", "Which version are you on?")]);
}
