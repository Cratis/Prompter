// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_DelayedForumSchedule.when_reading_configuration;

public class and_the_forum_is_the_immediate_help_forum : Specification
{
    IReadOnlyList<DelayedForum> _result = null!;

    void Because() => _result = DelayedForumSchedule.Configured(new DiscordOptions
    {
        HelpForumChannelId = 5,
        DelayedForums = [new() { ChannelId = 5 }, new() { ChannelId = 6 }]
    });

    [Fact] void should_leave_the_help_forum_to_the_immediate_path() => _result.Select(forum => forum.ChannelId).ShouldEqual([6UL]);
}
