// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_Mentions.when_receiving_a_forum_message;

public class and_another_participant_replies_to_prompter : given.an_admitted_message
{
    void Establish() => Message = Build(200, 70, 9, "does that work for tenants too?", referenced: Json(150, 70, 42, "Here is how.", authorIsBot: true), channel: Thread());

    async Task Because() => await Mentions.HandleAsync(Message);

    [Fact] void should_answer() => Answers.ReceivedCalls().Count().ShouldEqual(1);
    [Fact] void should_label_the_source_as_a_reply() => Source().ShouldEqual("discord-reply");
}
