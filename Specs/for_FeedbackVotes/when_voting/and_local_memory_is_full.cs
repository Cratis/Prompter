// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;
using Cratis.Prompter.Storage;

namespace Cratis.Prompter.Specs.for_FeedbackVotes.when_voting;

public class and_local_memory_is_full : given.a_feedback_budget
{
    bool _otherUser;
    FeedbackVote _otherAnswer;

    void Because()
    {
        Options.Protection.LocalCapacity = 1;
        Create();
        Votes.TrySpend(7);
        Votes.Remember(new(FeedbackVerdict.Up, 42));
        _otherUser = Votes.TrySpend(8);
        _otherAnswer = Votes.Remember(new(FeedbackVerdict.Up, 43));
    }

    [Fact] void should_refuse_a_new_user() => _otherUser.ShouldBeFalse();
    [Fact] void should_refuse_a_new_answer() => _otherAnswer.ShouldEqual(FeedbackVote.Refused);
}
