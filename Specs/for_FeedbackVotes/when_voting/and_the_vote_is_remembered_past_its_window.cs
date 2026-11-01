// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_FeedbackVotes.when_voting;

public class and_the_vote_is_remembered_past_its_window : given.a_feedback_budget
{
    FeedbackVote _result;

    void Because()
    {
        Create();
        Votes.Commit(new(FeedbackVerdict.Up, 42));
        Clock.Advance(TimeSpan.FromSeconds(600));
        _result = Votes.Check(new(FeedbackVerdict.Up, 42));
    }

    [Fact] void should_record_it_again_after_the_window() => _result.ShouldEqual(FeedbackVote.Record);
}
