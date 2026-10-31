// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_Mentions.when_receiving_a_follow_up;

public class and_it_is_a_duplicate : given.an_admitted_message
{
    void Establish() => Admission.TryEnter(new(1, 2, 3), "current question");

    async Task Because() => await Mentions.HandleAsync(Message);

    [Fact] void should_not_read_history() => History.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_reserve_spend() => Ledger.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_answer() => Answers.ReceivedCalls().ShouldBeEmpty();
}
