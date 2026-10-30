// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Mentions.when_receiving_a_follow_up;

public class and_spend_is_refused : given.an_admitted_message
{
    void Establish() => Ledger.Reserve(Arg.Any<SpendExposure>(), Arg.Any<CancellationToken>()).Returns((Guid?)null);

    async Task Because() => await Mentions.HandleAsync(Message);

    [Fact] void should_attempt_reservation() => Ledger.ReceivedCalls().Count().ShouldEqual(1);
    [Fact] void should_not_read_history() => History.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_answer() => Answers.ReceivedCalls().ShouldBeEmpty();
}
