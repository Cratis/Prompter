// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NetCord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Mentions.when_receiving_a_reply;

public class and_it_forwards_a_prompter_message : given.an_admitted_message
{
    void Establish() => Message = Build(100, 5, 3, "look at this", referenced: Json(50, 5, 42, "Which version are you on?", authorIsBot: true), referenceType: MessageReferenceType.Forward);

    async Task Because() => await Mentions.HandleAsync(Message);

    [Fact] void should_not_answer() => Answers.ReceivedCalls().ShouldBeEmpty();
}
