// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_Mentions.when_receiving_a_reply;

public class and_the_user_is_rate_limited : given.an_admitted_message
{
    void Establish()
    {
        for (var index = 0; index < 20; index++) Admission.TryEnter(new(1, 5, 3), $"another question {index}");
        Message = Build(100, 5, 3, "what about version 2?", referenced: Json(50, 5, 42, "Which version are you on?", authorIsBot: true));
    }

    async Task Because() => await Mentions.HandleAsync(Message);

    [Fact] void should_not_reserve_spend() => Ledger.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_answer() => Answers.ReceivedCalls().ShouldBeEmpty();
}
