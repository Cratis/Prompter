// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Digest;

namespace Cratis.Prompter.Specs.for_DigestSchedule.when_finding_the_next_slot;

public class and_the_hour_is_out_of_range : Specification
{
    DateTimeOffset _early;
    DateTimeOffset _late;
    void Because()
    {
        var now = new DateTimeOffset(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);
        _early = DigestSchedule.NextSlot(now, DayOfWeek.Monday, -1);
        _late = DigestSchedule.NextSlot(now, DayOfWeek.Monday, 24);
    }
    [Fact] void should_clamp_negative_hours() => _early.Hour.ShouldEqual(0);
    [Fact] void should_clamp_large_hours() => _late.Hour.ShouldEqual(23);
}
