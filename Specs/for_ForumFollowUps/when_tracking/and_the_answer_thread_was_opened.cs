// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_ForumFollowUps.when_tracking;

public class and_the_answer_thread_was_opened : given.forum_follow_ups
{
    void Because()
    {
        FollowUps.Opened(900, 3);
        FollowUps.HandOff(900);
    }

    [Fact] void should_remember_the_asker() => FollowUps.AskerOf(900).ShouldEqual(3UL);
    [Fact] void should_stop_accepting_after_hand_off() => FollowUps.Accepts(900).ShouldBeFalse();
    [Fact] void should_not_know_an_untracked_thread() => FollowUps.AskerOf(901).ShouldBeNull();
}
