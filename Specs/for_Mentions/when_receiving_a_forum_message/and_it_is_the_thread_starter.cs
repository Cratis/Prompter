// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_Mentions.when_receiving_a_forum_message;

public class and_it_is_the_thread_starter : given.an_admitted_message
{
    void Establish() => Message = Build(70, 70, 3, "<@42> how do I get started?", channel: Thread());

    async Task Because() => await Mentions.HandleAsync(Message);

    [Fact] void should_leave_the_starter_to_the_help_forum() => Answers.ReceivedCalls().ShouldBeEmpty();
}
