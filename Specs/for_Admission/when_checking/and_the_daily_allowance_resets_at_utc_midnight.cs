// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_Admission.when_checking;

public class and_the_daily_allowance_resets_at_utc_midnight : given.an_admission
{
    AdmissionDecision _spent = null!;
    AdmissionDecision _beforeMidnight = null!;
    AdmissionDecision _afterMidnight = null!;

    void Establish()
    {
        Options.Protection.UserAdmissionsPerDay = 1;
        Clock.Advance(TimeSpan.FromHours(23));
    }

    void Because()
    {
        Guard.TryEnter(new(123, 1, 1), "first question");
        Clock.Advance(TimeSpan.FromMinutes(30));
        _spent = Guard.TryEnter(new(123, 1, 1), "second question");
        Clock.Advance(TimeSpan.FromMinutes(29));
        _beforeMidnight = Guard.TryEnter(new(123, 1, 1), "third question");
        Clock.Advance(TimeSpan.FromMinutes(1));
        _afterMidnight = Guard.TryEnter(new(123, 1, 1), "fourth question");
    }

    [Fact] void should_refuse_once_the_allowance_is_spent() => _spent.Allowed.ShouldBeFalse();
    [Fact] void should_still_refuse_before_midnight() => _beforeMidnight.Allowed.ShouldBeFalse();
    [Fact] void should_admit_after_midnight_utc() => _afterMidnight.Allowed.ShouldBeTrue();
}
