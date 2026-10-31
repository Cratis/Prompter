// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_DelayedForumSchedule.when_reading_configuration;

public class and_the_delay_is_not_set : Specification
{
    IReadOnlyList<DelayedForum> _result = null!;

    void Because() => _result = DelayedForumSchedule.Configured(new DiscordOptions
    {
        DelayedForums = [new() { ChannelId = 5 }]
    });

    [Fact] void should_default_to_thirty_minutes() => _result.Single().Delay.ShouldEqual(TimeSpan.FromMinutes(30));
}
