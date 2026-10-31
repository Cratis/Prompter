// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_DelayedForumSchedule.when_reading_configuration;

public class and_a_forum_is_listed_twice : Specification
{
    IReadOnlyList<DelayedForum> _result = null!;

    void Because() => _result = DelayedForumSchedule.Configured(new DiscordOptions
    {
        DelayedForums = [new() { ChannelId = 5, DelayMinutes = 10 }, new() { ChannelId = 5, DelayMinutes = 20 }]
    });

    [Fact] void should_keep_the_first_entry() => _result.Single().ShouldEqual(new DelayedForum(5, TimeSpan.FromMinutes(10)));
}
