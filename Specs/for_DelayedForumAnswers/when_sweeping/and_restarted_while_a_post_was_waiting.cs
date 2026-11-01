// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_DelayedForumAnswers.when_sweeping;

public class and_restarted_while_a_post_was_waiting : given.delayed_forum_answers
{
    void Establish() => Post(70, 600);
    async Task Because()
    {
        using var restarted = Create();
        Answered = await restarted.SweepOnce(CancellationToken.None);
    }

    [Fact] void should_answer_the_overdue_post_once() => Answered.ShouldEqual(1);
}
