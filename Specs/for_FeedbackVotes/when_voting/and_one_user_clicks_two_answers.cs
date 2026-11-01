// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_FeedbackVotes.when_voting;

public class and_one_user_clicks_two_answers : given.a_feedback_budget
{
    bool _secondWaited;

    async Task Because()
    {
        Create();
        var first = (await Votes.Enter(42, 7))!;
        var second = Votes.Enter(43, 7);
        _secondWaited = !second.IsCompleted;
        first.Dispose();
        (await second)!.Dispose();
    }

    [Fact] void should_serialize_the_users_clicks() => _secondWaited.ShouldBeTrue();
}
