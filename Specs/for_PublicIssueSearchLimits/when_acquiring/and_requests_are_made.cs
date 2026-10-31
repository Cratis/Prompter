// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;
using Cratis.Prompter.Specs.Fakes;

namespace Cratis.Prompter.Specs.for_PublicIssueSearchLimits.when_acquiring;

public class and_requests_are_made : Specification
{
    ControlledTime _time = null!;
    PublicIssueSearchLimits _limits = null!;
    int _granted;
    bool _afterMinute;
    bool _afterCooldown;
    bool _duringCooldown;

    void Establish()
    {
        _time = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        _limits = new(_time);
    }

    void Because()
    {
        _granted = Enumerable.Range(0, 20).Count(_ => _limits.TryAcquire());
        _time.Advance(TimeSpan.FromSeconds(61));
        _afterMinute = _limits.TryAcquire();
        _limits.CoolDown(TimeSpan.FromHours(5));
        _time.Advance(TimeSpan.FromMinutes(14));
        _duringCooldown = _limits.TryAcquire();
        _time.Advance(TimeSpan.FromMinutes(2));
        _afterCooldown = _limits.TryAcquire();
    }

    [Fact] void should_allow_at_most_eight_per_minute() => _granted.ShouldEqual(8);
    [Fact] void should_allow_again_after_the_minute() => _afterMinute.ShouldBeTrue();
    [Fact] void should_cap_a_long_cooldown() => _duringCooldown.ShouldBeFalse();
    [Fact] void should_resume_after_the_capped_cooldown() => _afterCooldown.ShouldBeTrue();
}
