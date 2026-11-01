// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_FeedbackVotes.when_voting;

public class and_another_user_repeats_the_vote : given.a_feedback_budget
{
    FeedbackVote _result;

    void Because()
    {
        Create();
        Votes.TrySpend(7);
        Votes.Commit(new(FeedbackVerdict.Up, 42));
        Votes.TrySpend(8);
        _result = Votes.Check(new(FeedbackVerdict.Up, 42));
    }

    [Fact] void should_not_record_it_again() => _result.ShouldEqual(FeedbackVote.Repeat);
}
