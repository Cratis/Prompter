// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_ForumFollowUps.when_tracking;

public class and_the_asker_expired : given.forum_follow_ups
{
    void Because()
    {
        FollowUps.Opened(900, 3);
        Clock.Advance(Cratis.Prompter.Discord.ForumFollowUps.Lifetime + TimeSpan.FromSeconds(1));
    }

    [Fact] void should_forget_the_asker() => FollowUps.AskerOf(900).ShouldBeNull();
}
