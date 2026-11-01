// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_FeedbackVotes.when_voting;

public class and_a_write_failed : given.a_feedback_budget
{
    FeedbackVote _result;

    void Because()
    {
        Create();
        var click = new FeedbackClick(FeedbackVerdict.Up, 42);
        Votes.Check(click);
        _result = Votes.Check(click);
    }

    [Fact] void should_record_the_retry_because_nothing_was_committed() => _result.ShouldEqual(FeedbackVote.Record);
}
