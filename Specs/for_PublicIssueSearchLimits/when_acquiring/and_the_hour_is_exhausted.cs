// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.GitHub;
using Cratis.Prompter.Specs.Fakes;

namespace Cratis.Prompter.Specs.for_PublicIssueSearchLimits.when_acquiring;

public class and_the_hour_is_exhausted : Specification
{
    int _granted;

    void Because()
    {
        var time = new ControlledTime(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var limits = new PublicIssueSearchLimits(time);
        for (var minute = 0; minute < 10; minute++)
        {
            _granted += Enumerable.Range(0, 8).Count(_ => limits.TryAcquire());
            time.Advance(TimeSpan.FromSeconds(61));
        }
    }

    [Fact] void should_allow_at_most_forty_per_hour() => _granted.ShouldEqual(40);
}
