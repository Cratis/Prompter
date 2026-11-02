// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_Admission.when_checking;

public class and_the_requester_is_in_cooldown : given.an_admission
{
    AdmissionDecision _first = null!;
    AdmissionDecision _denied = null!;
    AdmissionDecision _other = null!;
    AdmissionDecision _expired = null!;

    void Establish() => Options.Protection.UserAdmissionsPerDay = 2;

    void Because()
    {
        _first = Guard.TryEnter(new(123, 1, 1), "one question");
        Clock.Advance(TimeSpan.FromSeconds(59));
        _denied = Guard.TryEnter(new(123, 2, 1), "another question");
        _other = Guard.TryEnter(new(123, 2, 2), "another question");
        Clock.Advance(TimeSpan.FromSeconds(1));
        _expired = Guard.TryEnter(new(123, 3, 1), "another question");
    }

    [Fact] void should_admit_the_first_request() => _first.Allowed.ShouldBeTrue();
    [Fact] void should_deny_a_different_question_in_another_channel() => _denied.Allowed.ShouldBeFalse();
    [Fact] void should_allow_another_requester() => _other.Allowed.ShouldBeTrue();
    [Fact] void should_not_extend_cooldown_or_spend_daily_capacity_on_denial() => _expired.Allowed.ShouldBeTrue();
}
