// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Digest;

namespace Cratis.Prompter.Specs.for_DigestSchedule.when_finding_the_next_slot;

public class and_now_is_exactly_the_slot : Specification
{
    DateTimeOffset _slot;
    void Because() => _slot = DigestSchedule.NextSlot(new(2026, 11, 2, 8, 0, 0, TimeSpan.Zero), DayOfWeek.Monday, 8);
    [Fact] void should_skip_to_next_week() => _slot.ShouldEqual(new DateTimeOffset(2026, 11, 9, 8, 0, 0, TimeSpan.Zero));
}
