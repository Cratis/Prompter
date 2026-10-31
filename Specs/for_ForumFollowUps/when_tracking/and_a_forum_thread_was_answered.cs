// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_ForumFollowUps.when_tracking;

public class and_a_forum_thread_was_answered : given.forum_follow_ups
{
    void Because() => FollowUps.Answered(70);

    [Fact] void should_have_no_asker() => FollowUps.AskerOf(70).ShouldBeNull();
}
