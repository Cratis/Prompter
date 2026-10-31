// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_Mentions.when_receiving_a_reply;

public class and_prompter_replies_to_itself : given.an_admitted_message
{
    void Establish() => Message = Build(100, 5, 42, "my own follow-up", authorIsBot: true, referenced: Json(50, 5, 42, "Which version are you on?", authorIsBot: true));

    async Task Because() => await Mentions.HandleAsync(Message);

    [Fact] void should_not_answer() => Answers.ReceivedCalls().ShouldBeEmpty();
}
