// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_ForumFollowUps.when_tracking;

public class and_capacity_is_reached : given.forum_follow_ups
{
    void Establish() => Options.Protection.LocalCapacity = 2;

    void Because()
    {
        FollowUps.Answered(1);
        Clock.Advance(TimeSpan.FromMinutes(1));
        FollowUps.Answered(2);
        Clock.Advance(TimeSpan.FromMinutes(1));
        FollowUps.Answered(3);
    }

    [Fact] void should_evict_the_oldest_thread() => FollowUps.Accepts(1).ShouldBeFalse();
    [Fact] void should_keep_the_newer_threads() => (FollowUps.Accepts(2) && FollowUps.Accepts(3)).ShouldBeTrue();
}
