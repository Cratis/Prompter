// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_Mentions.when_receiving_a_reply;

public class and_it_replies_to_another_user : given.an_admitted_message
{
    void Establish() => Message = Build(100, 5, 3, "thanks, that helps", referenced: Json(50, 5, 9, "try restarting"));

    async Task Because() => await Mentions.HandleAsync(Message);

    [Fact] void should_not_answer() => Answers.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_read_history() => History.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_reserve_spend() => Ledger.ReceivedCalls().ShouldBeEmpty();
}
