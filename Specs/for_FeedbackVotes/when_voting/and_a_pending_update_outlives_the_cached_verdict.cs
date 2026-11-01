// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_FeedbackVotes.when_voting;

public class and_a_pending_update_outlives_the_cached_verdict : given.a_feedback_budget
{
    FeedbackVote _duringWrite;
    FeedbackVote _afterFailure;

    async Task Because()
    {
        Options.Protection.LocalCapacity = 2;
        Create();
        Votes.Commit(new(FeedbackVerdict.Up, 42));
        Votes.Commit(new(FeedbackVerdict.Up, 43));
        using (var pending = await Votes.Enter(42, 7))
        {
            Votes.Check(new(FeedbackVerdict.Down, 42));
            Clock.Advance(TimeSpan.FromSeconds(600));
            Votes.Commit(new(FeedbackVerdict.Up, 43));
            _duringWrite = Votes.Check(new(FeedbackVerdict.Up, 44));
        }

        _afterFailure = Votes.Check(new(FeedbackVerdict.Up, 44));
    }

    [Fact] void should_not_expire_capacity_reserved_by_an_update() => _duringWrite.ShouldEqual(FeedbackVote.Refused);
    [Fact] void should_allow_expiry_after_the_failed_update_releases_its_slot() => _afterFailure.ShouldEqual(FeedbackVote.Record);
}
