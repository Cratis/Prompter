// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_FeedbackVotes.when_voting;

public class and_the_first_vote_arrives : given.a_feedback_budget
{
    FeedbackVote _result;

    void Because()
    {
        Create();
        _result = Votes.Remember(new(FeedbackVerdict.Up, 42));
    }

    [Fact] void should_record_it() => _result.ShouldEqual(FeedbackVote.Record);
}
