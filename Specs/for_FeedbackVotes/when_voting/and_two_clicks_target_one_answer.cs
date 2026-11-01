// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_FeedbackVotes.when_voting;

public class and_two_clicks_target_one_answer : given.a_feedback_budget
{
    FeedbackTurn _first = null!;
    Task<FeedbackTurn?> _second = null!;
    FeedbackTurn? _other;
    bool _secondWaitedForTheFirst;

    async Task Because()
    {
        Create();
        _first = (await Votes.Enter(42, 7))!;
        _second = Votes.Enter(42, 8);
        _secondWaitedForTheFirst = !_second.IsCompleted;
        _other = await Votes.Enter(43, 9);
        _first.Dispose();
        (await _second)!.Dispose();
        _other!.Dispose();
    }

    [Fact] void should_make_the_second_click_wait_for_the_first() => _secondWaitedForTheFirst.ShouldBeTrue();
    [Fact] void should_not_block_a_different_answer() => _other.ShouldNotBeNull();
}
