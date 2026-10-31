// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_ForumFollowUps.when_tracking;

public class and_the_answer_expired : given.forum_follow_ups
{
    void Because()
    {
        FollowUps.Answered(70);
        Clock.Advance(ForumFollowUps.Lifetime + TimeSpan.FromSeconds(1));
    }

    [Fact] void should_not_accept_the_thread() => FollowUps.Accepts(70).ShouldBeFalse();
}
