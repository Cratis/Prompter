// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_Admission.when_checking;

public class and_questions_are_fifteen_seconds_apart : given.an_admission
{
    AdmissionDecision _first = null!;
    AdmissionDecision _tooSoon = null!;
    AdmissionDecision _second = null!;

    void Because()
    {
        _first = Guard.TryEnter(new(123, 1, 1), "first question");
        Clock.Advance(TimeSpan.FromSeconds(14));
        _tooSoon = Guard.TryEnter(new(123, 1, 1), "second question");
        Clock.Advance(TimeSpan.FromSeconds(1));
        _second = Guard.TryEnter(new(123, 1, 1), "second question");
    }

    [Fact] void should_admit_the_first_question() => _first.Allowed.ShouldBeTrue();
    [Fact] void should_refuse_a_question_inside_the_default_cooldown() => _tooSoon.Allowed.ShouldBeFalse();
    [Fact] void should_admit_a_question_after_fifteen_seconds() => _second.Allowed.ShouldBeTrue();
}
