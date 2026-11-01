// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Deployment.Services;

namespace Cratis.Prompter.Specs.for_DiscordBehaviorEnvironment.when_projecting;

public class and_a_forum_delay_is_invalid : Specification
{
    Exception? _zero;
    Exception? _tooLong;
    Exception? _notANumber;
    Exception? _badId;

    void Because()
    {
        _zero = Catch.Exception(() => DiscordBehaviorEnvironment.Values(null, null, "10:0"));
        _tooLong = Catch.Exception(() => DiscordBehaviorEnvironment.Values(null, null, "10:1441"));
        _notANumber = Catch.Exception(() => DiscordBehaviorEnvironment.Values(null, null, "10:x"));
        _badId = Catch.Exception(() => DiscordBehaviorEnvironment.Values(null, null, "x:30"));
    }

    [Fact] void should_reject_zero_minutes() => _zero.ShouldBeOfExactType<InvalidForumDelay>();
    [Fact] void should_reject_more_than_a_day() => _tooLong.ShouldBeOfExactType<InvalidForumDelay>();
    [Fact] void should_reject_a_non_number() => _notANumber.ShouldBeOfExactType<InvalidForumDelay>();
    [Fact] void should_reject_a_bad_forum_id() => _badId.ShouldBeOfExactType<InvalidDiscordId>();
}
