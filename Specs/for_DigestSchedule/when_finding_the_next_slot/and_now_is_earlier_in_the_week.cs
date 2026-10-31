// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Digest;

namespace Cratis.Prompter.Specs.for_DigestSchedule.when_finding_the_next_slot;

public class and_now_is_earlier_in_the_week : Specification
{
    DateTimeOffset _slot;
    void Because() => _slot = DigestSchedule.NextSlot(new(2026, 11, 1, 12, 0, 0, TimeSpan.FromHours(2)), DayOfWeek.Monday, 8);
    [Fact] void should_find_monday_in_utc() => _slot.ShouldEqual(new DateTimeOffset(2026, 11, 2, 8, 0, 0, TimeSpan.Zero));
    [Fact] void should_use_the_preceding_seven_days() => DigestSchedule.WindowFor(_slot).ShouldEqual((_slot.AddDays(-7), _slot));
}
