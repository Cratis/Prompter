// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_Mentions.when_receiving_a_forum_message;

public class and_a_helper_joined_before_the_follow_up : given.an_admitted_message
{
    void Establish()
    {
        FollowUps.Answered(70);
        FollowUps.HandOff(70);
        Message = Build(200, 70, 3, "and what about version 2?", channel: Thread());
    }

    async Task Because() => await Mentions.HandleAsync(Message);

    [Fact] void should_not_answer() => Answers.ReceivedCalls().ShouldBeEmpty();
}
