// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_DelayedForumSchedule.when_reading_configuration;

public class and_the_delay_is_out_of_range : Specification
{
    IReadOnlyList<DelayedForum> _result = null!;

    void Because() => _result = DelayedForumSchedule.Configured(new DiscordOptions
    {
        DelayedForums = [new() { ChannelId = 5, DelayMinutes = 0 }, new() { ChannelId = 6, DelayMinutes = 1441 }, new() { ChannelId = 7, DelayMinutes = 1440 }]
    });

    [Fact] void should_keep_only_the_valid_forum() => _result.Select(forum => forum.ChannelId).ShouldEqual([7UL]);
}
