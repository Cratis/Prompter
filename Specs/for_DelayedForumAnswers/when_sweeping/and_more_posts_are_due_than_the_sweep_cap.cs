// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_DelayedForumAnswers.when_sweeping;

public class and_more_posts_are_due_than_the_sweep_cap : given.delayed_forum_answers
{
    void Establish()
    {
        Post(74, 40);
        Post(72, 60);
        Post(71, 70);
        Post(73, 50);
    }

    async Task Because() => Answered = await Sweeper.SweepOnce(CancellationToken.None);

    [Fact] void should_answer_only_the_cap() => Answered.ShouldEqual(DelayedForumAnswers.MaximumAnswersPerSweep);
    [Fact] void should_answer_the_oldest_first() => AnsweredThreads().ShouldEqual([71UL, 72UL, 73UL]);
}
