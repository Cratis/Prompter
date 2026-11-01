// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_FeedbackVotes.when_voting;

public class and_too_many_answers_are_in_flight : given.a_feedback_budget
{
    FeedbackTurn? _overflow;

    async Task Because()
    {
        Options.Protection.LocalCapacity = 1;
        Create();
        var first = await Votes.Enter(42, 7);
        _overflow = await Votes.Enter(43, 8);
        first!.Dispose();
    }

    [Fact] void should_refuse_the_extra_answer() => _overflow.ShouldBeNull();
}
