// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Protection;

namespace Cratis.Prompter.Specs.for_ProtectionOptions.when_validating;

public class and_nonpaid_limits_are_invalid : Specification
{
    bool _invalidDenied;
    bool _zeroDeniesWork;
    bool _upperBoundsAllowed;

    void Because()
    {
        ProtectionOptions[] invalid =
        [
            new() { ActionConcurrency = 0 }, new() { ActionConcurrency = 9 },
            new() { ActionPerMinute = -1 }, new() { ActionPerMinute = 121 },
            new() { ActionChannelPerMinute = -1 }, new() { ActionChannelPerMinute = 121 },
            new() { ActionClicksPerUser = -1 }, new() { ActionClicksPerUser = 101 },
            new() { ActionWindowSeconds = 0 }, new() { ActionWindowSeconds = 86401 }
        ];
        _invalidDenied = invalid.All(options => !options.IsValid);
        _zeroDeniesWork = new ProtectionOptions { ActionPerMinute = 0, ActionChannelPerMinute = 0, ActionClicksPerUser = 0 }.IsValid;
        _upperBoundsAllowed = new ProtectionOptions { ActionConcurrency = 8, ActionPerMinute = 120, ActionChannelPerMinute = 120, ActionClicksPerUser = 100, ActionWindowSeconds = 86400 }.IsValid;
    }

    [Fact] void should_reject_invalid_limits() => _invalidDenied.ShouldBeTrue();
    [Fact] void should_allow_zero_throughput_as_an_explicit_denial() => _zeroDeniesWork.ShouldBeTrue();
    [Fact] void should_allow_the_documented_upper_bounds() => _upperBoundsAllowed.ShouldBeTrue();
}
