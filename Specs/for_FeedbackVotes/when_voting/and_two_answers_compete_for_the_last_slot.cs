// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_FeedbackVotes.when_voting;

public class and_two_answers_compete_for_the_last_slot : given.a_feedback_budget
{
    FeedbackVote _second;
    FeedbackVote _repeat;
    FeedbackVote _afterFailure;
    FeedbackVote _afterRecovery;

    async Task Because()
    {
        Options.Protection.LocalCapacity = 2;
        Create();
        Votes.Commit(new(FeedbackVerdict.Up, 42));
        using var first = await Votes.Enter(43, 7);
        Votes.Check(new(FeedbackVerdict.Up, 43));
        using (var second = await Votes.Enter(44, 8))
        {
            _second = Votes.Check(new(FeedbackVerdict.Up, 44));
        }

        Votes.Commit(new(FeedbackVerdict.Up, 43));
        _repeat = Votes.Check(new(FeedbackVerdict.Up, 43));
        first!.Dispose();
        Clock.Advance(TimeSpan.FromSeconds(600));
        Votes.Commit(new(FeedbackVerdict.Up, 42));
        using (var failing = await Votes.Enter(45, 7))
        {
            Votes.Check(new(FeedbackVerdict.Up, 45));

            // A failed write never commits; disposing its turn must release its reservation.
        }

        using var recovery = await Votes.Enter(46, 8);
        _afterFailure = Votes.Check(new(FeedbackVerdict.Up, 46));
        Votes.Commit(new(FeedbackVerdict.Up, 46));
        _afterRecovery = Votes.Check(new(FeedbackVerdict.Up, 46));
    }

    [Fact] void should_refuse_the_competing_answer_while_the_first_is_pending() => _second.ShouldEqual(FeedbackVote.Refused);
    [Fact] void should_remember_the_successful_answer() => _repeat.ShouldEqual(FeedbackVote.Repeat);
    [Fact] void should_release_the_failed_answers_slot() => _afterFailure.ShouldEqual(FeedbackVote.Record);
    [Fact] void should_remember_the_recovery() => _afterRecovery.ShouldEqual(FeedbackVote.Repeat);
}
