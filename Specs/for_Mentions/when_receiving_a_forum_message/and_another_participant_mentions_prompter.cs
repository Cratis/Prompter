// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_Mentions.when_receiving_a_forum_message;

public class and_another_participant_mentions_prompter : given.an_admitted_message
{
    void Establish()
    {
        FollowUps.Answered(70);
        Message = Build(200, 70, 9, "<@42> how do I configure this?", channel: Thread());
    }

    async Task Because() => await Mentions.HandleAsync(Message);

    [Fact] void should_answer() => Answers.ReceivedCalls().Count().ShouldEqual(1);
    [Fact] void should_label_the_source_as_a_mention() => Source().ShouldEqual("discord-mention");
    [Fact] void should_keep_following_the_thread() => FollowUps.Accepts(70).ShouldBeTrue();
}
