// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_ForumFollowUps.when_tracking;

public class and_a_helper_posts_in_an_untracked_thread : given.forum_follow_ups
{
    void Because()
    {
        FollowUps.HandOff(70);
        FollowUps.Answered(70);
    }

    [Fact] void should_accept_the_thread_once_answered() => FollowUps.Accepts(70).ShouldBeTrue();
}
