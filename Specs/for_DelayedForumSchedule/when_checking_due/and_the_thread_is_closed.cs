// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_DelayedForumSchedule.when_checking_due;

public class and_the_thread_is_closed : Specification
{
    static readonly DateTimeOffset _now = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);
    bool _result;

    void Because() => _result = DelayedForumSchedule.IsDue(new(1, 1, 2, 3, _now.AddMinutes(-45), true), TimeSpan.FromMinutes(30), _now);

    [Fact] void should_not_be_due() => _result.ShouldBeFalse();
}
