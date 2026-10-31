// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_DelayedForumAnswers.when_sweeping;

public class and_swept_twice : given.delayed_forum_answers
{
    int _second;

    void Establish() => Post(70, 45);

    async Task Because()
    {
        Answered = await Sweeper.SweepOnce(CancellationToken.None);
        _second = await Sweeper.SweepOnce(CancellationToken.None);
    }

    [Fact] void should_answer_it_the_first_time() => Answered.ShouldEqual(1);
    [Fact] void should_not_answer_it_again() => _second.ShouldEqual(0);
}
