// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_ProtectionOptions.when_validating;

public class and_requester_fairness_limits_are_configured : Specification
{
    bool _invalid;
    bool _valid;

    void Because()
    {
        ProtectionOptions[] invalid =
        [
            new() { UserAdmissionsPerDay = -1 }, new() { UserAdmissionsPerDay = 101 },
            new() { UserAdmissionCooldownSeconds = 0 }, new() { UserAdmissionCooldownSeconds = 86401 }
        ];
        _invalid = invalid.All(options => !options.IsValid);
        _valid = new ProtectionOptions { UserAdmissionsPerDay = 0, UserAdmissionCooldownSeconds = 1 }.IsValid &&
            new ProtectionOptions { UserAdmissionsPerDay = 100, UserAdmissionCooldownSeconds = 86400 }.IsValid;
    }

    [Fact] void should_validate_the_documented_bounds() => _invalid.ShouldBeTrue();
    [Fact] void should_accept_the_documented_boundaries() => _valid.ShouldBeTrue();
    [Fact] void should_default_to_thirty_admissions_per_day() => new ProtectionOptions().UserAdmissionsPerDay.ShouldEqual(30);
    [Fact] void should_default_to_fifteen_seconds_between_admissions() => new ProtectionOptions().UserAdmissionCooldownSeconds.ShouldEqual(15);
}
