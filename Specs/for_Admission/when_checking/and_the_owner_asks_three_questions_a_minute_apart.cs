// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_Admission.when_checking;

public class and_the_owner_asks_three_questions_a_minute_apart : given.an_admission
{
    readonly List<AdmissionDecision> _decisions = [];

    void Because()
    {
        for (var i = 0; i < 3; i++)
        {
            _decisions.Add(Guard.TryEnter(new(123, 1, 1), $"question number {i}"));
            Clock.Advance(TimeSpan.FromSeconds(60));
        }
    }

    [Fact] void should_admit_all_three() => _decisions.TrueForAll(decision => decision.Allowed).ShouldBeTrue();
}
