// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_Mentions.when_receiving_a_forum_message;

public class and_the_author_follows_up_after_an_answer : given.an_admitted_message
{
    void Establish()
    {
        FollowUps.Answered(70);
        Message = Build(200, 70, 3, "and what about version 2?", channel: Thread());
    }

    async Task Because() => await Mentions.HandleAsync(Message);

    [Fact] void should_answer_once() => Answers.ReceivedCalls().Count().ShouldEqual(1);
    [Fact] void should_ask_the_follow_up() => Asked().Value.ShouldEqual("and what about version 2?");
    [Fact] void should_label_the_source_as_a_forum_follow_up() => Source().ShouldEqual("discord-forum-follow-up");
}
