// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_FeedbackVotes.when_voting;

public class and_the_vote_changes : given.a_feedback_budget
{
    FeedbackVote[] _results = null!;

    void Because()
    {
        Create();
        _results = [Votes.Remember(new(FeedbackVerdict.Up, 42)), Votes.Remember(new(FeedbackVerdict.Down, 42)), Votes.Remember(new(FeedbackVerdict.Up, 42))];
    }

    [Fact] void should_record_the_latest_verdict_each_time() => _results.All(result => result == FeedbackVote.Record).ShouldBeTrue();
}
