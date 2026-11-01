// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_FeedbackVotes.when_voting;

public class and_a_click_waits_past_its_turn_timeout : given.a_feedback_budget
{
    FeedbackTurn? _late;
    FeedbackTurn? _afterwards;

    async Task Because()
    {
        Create();
        var first = (await Votes.Enter(42, 7))!;
        var pending = Votes.Enter(42, 8);
        Clock.Advance(TimeSpan.FromSeconds(2));
        _late = await pending;
        first.Dispose();
        _afterwards = await Votes.Enter(42, 8);
        _afterwards!.Dispose();
    }

    [Fact] void should_refuse_the_late_click() => _late.ShouldBeNull();
    [Fact] void should_free_the_answer_for_later_clicks() => _afterwards.ShouldNotBeNull();
}
