// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_ForumFollowUps.when_tracking;

public class and_the_thread_was_never_answered : given.forum_follow_ups
{
    [Fact] void should_not_accept_the_thread() => FollowUps.Accepts(70).ShouldBeFalse();
}
