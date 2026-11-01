// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_DelayedForumAnswers.when_sweeping;

public class and_a_post_is_due_and_unanswered : given.delayed_forum_answers
{
    void Establish() => Post(70, 45);
    async Task Because() => Answered = await Sweeper.SweepOnce(CancellationToken.None);

    [Fact] void should_answer_one_post() => Answered.ShouldEqual(1);
    [Fact] void should_answer_it_as_a_delayed_answer() => Answerer.Received(1).Answer(Arg.Is<ForumThreadRef>(thread => thread.Id == 70 && thread.ParentId == Forum && thread.OwnerId == Owner && thread.GuildId == 1), Forum, null, true);
}
