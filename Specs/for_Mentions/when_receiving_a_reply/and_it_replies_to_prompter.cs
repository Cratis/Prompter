// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NetCord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Mentions.when_receiving_a_reply;

public class and_it_replies_to_prompter : given.an_admitted_message
{
    void Establish() => Message = Build(100, 5, 3, "what about version 2?", referenced: Json(50, 5, 42, "Which version are you on?", authorIsBot: true));

    async Task Because() => await Mentions.HandleAsync(Message);

    [Fact] void should_answer_once() => Answers.ReceivedCalls().Count().ShouldEqual(1);
    [Fact] void should_ask_the_reply_text() => Asked().Value.ShouldEqual("what about version 2?");
    [Fact] void should_label_the_source_as_a_reply() => Source().ShouldEqual("discord-reply");
}
