// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_ProtectionOptions.when_validating;

public class and_escalation_and_user_lists_are_configured : Specification
{
    bool _invalid;
    bool _valid;

    void Because()
    {
        ProtectionOptions[] invalid =
        [
            new() { RefusalStrikes = -1 }, new() { RefusalStrikes = 101 },
            new() { RefusalStrikeWindowSeconds = 0 }, new() { RefusalStrikeWindowSeconds = 86401 },
            new() { EscalatedCooldownSeconds = 0 }, new() { EscalatedCooldownSeconds = 86401 },
            new() { EscalatedCooldownSeconds = 600, MaxEscalatedCooldownSeconds = 599 }, new() { MaxEscalatedCooldownSeconds = 604801 },
            new() { AllowedUsers = [0] }, new() { DeniedUsers = [1, 0] }
        ];
        _invalid = invalid.All(options => !options.IsValid);
        _valid = new ProtectionOptions { RefusalStrikes = 0 }.IsValid &&
            new ProtectionOptions { EscalatedCooldownSeconds = 600, MaxEscalatedCooldownSeconds = 600, AllowedUsers = [1], DeniedUsers = [2] }.IsValid;
    }

    [Fact] void should_validate_the_documented_bounds() => _invalid.ShouldBeTrue();
    [Fact] void should_accept_the_documented_boundaries() => _valid.ShouldBeTrue();
    [Fact] void should_default_to_five_strikes() => new ProtectionOptions().RefusalStrikes.ShouldEqual(5);
    [Fact] void should_default_to_a_six_hour_cap() => new ProtectionOptions().MaxEscalatedCooldownSeconds.ShouldEqual(21600);
}
