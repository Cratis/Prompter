// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_Admission.when_checking;

public class and_the_requester_is_a_maintainer : given.an_admission
{
    readonly List<AdmissionDecision> _burst = [];
    AdmissionDecision _duplicate = null!;
    AdmissionDecision _regularInCooldown = null!;
    AdmissionDecision _busy = null!;

    void Establish()
    {
        Options.Protection.UserAdmissionsPerDay = 1;
        Options.Protection.UserAdmissionCooldownSeconds = 600;
        Options.Protection.GlobalPerHour = 13;
        Options.Protection.ChannelPerHour = 100;
        Options.Protection.DuplicateSeconds = 120;
    }

    void Because()
    {
        for (var i = 0; i < 12; i++)
        {
            _burst.Add(Guard.TryEnter(new(123, 1, 7, Maintainer: true), $"question {i}"));
        }

        _duplicate = Guard.TryEnter(new(123, 1, 7, Maintainer: true), "question 3");
        _regularInCooldown = Guard.TryEnter(new(123, 1, 8), "first");
        Guard.TryEnter(new(123, 1, 8), "second");
        _busy = Guard.TryEnter(new(123, 1, 7, Maintainer: true), "one more");
    }

    [Fact] void should_bypass_the_daily_cap_cooldown_and_bucket() => _burst.TrueForAll(decision => decision.Allowed).ShouldBeTrue();
    [Fact] void should_still_suppress_duplicates() => _duplicate.Allowed.ShouldBeFalse();
    [Fact] void should_admit_a_regular_requester_once() => _regularInCooldown.Allowed.ShouldBeTrue();
    [Fact] void should_still_be_bound_by_the_global_limit() => _busy.Allowed.ShouldBeFalse();
}
