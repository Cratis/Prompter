// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_DelayedForumAnswers.when_sweeping;

public class and_restarted_after_answering : given.delayed_forum_answers
{
    void Establish()
    {
        Post(70, 45);
        Messages(70, new ForumMessage(70, Owner, false, false, true), new ForumMessage(71, Bot, true, false, true));
    }

    async Task Because()
    {
        using var restarted = Create();
        Answered = await restarted.SweepOnce(CancellationToken.None);
    }

    [Fact] void should_not_answer_the_post_again() => Answered.ShouldEqual(0);
}
