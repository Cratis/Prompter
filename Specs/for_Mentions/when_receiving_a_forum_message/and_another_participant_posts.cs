// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_Mentions.when_receiving_a_forum_message;

public class and_another_participant_posts : given.an_admitted_message
{
    void Establish()
    {
        FollowUps.Answered(70);
        Message = Build(200, 70, 9, "have you tried restarting?", channel: Thread());
    }

    async Task Because() => await Mentions.HandleAsync(Message);

    [Fact] void should_not_answer() => Answers.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_hand_the_thread_off() => FollowUps.Accepts(70).ShouldBeFalse();
}
