// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_DelayedForumSchedule.when_reading_configuration;

public class and_nothing_is_configured : Specification
{
    IReadOnlyList<DelayedForum> _result = null!;

    void Because() => _result = DelayedForumSchedule.Configured(new DiscordOptions());

    [Fact] void should_have_no_forums() => _result.ShouldBeEmpty();
}
