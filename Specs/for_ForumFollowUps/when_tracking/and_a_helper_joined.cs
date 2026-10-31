// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_ForumFollowUps.when_tracking;

public class and_a_helper_joined : given.forum_follow_ups
{
    bool _acceptedAfterHandOff;

    void Because()
    {
        FollowUps.Answered(70);
        FollowUps.HandOff(70);
        _acceptedAfterHandOff = FollowUps.Accepts(70);
        FollowUps.Answered(70);
    }

    [Fact] void should_stop_accepting_the_thread() => _acceptedAfterHandOff.ShouldBeFalse();
    [Fact] void should_stay_handed_off_after_another_answer() => FollowUps.Accepts(70).ShouldBeFalse();
}
