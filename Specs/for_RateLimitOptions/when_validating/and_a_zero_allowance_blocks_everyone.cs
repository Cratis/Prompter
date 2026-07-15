// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_RateLimitOptions.when_validating;

public class and_a_zero_allowance_blocks_everyone : Specification
{
    RateLimitOptions _options = null!;
    bool _valid;

    void Establish() => _options = new RateLimitOptions { MaxQuestions = 0, WindowMinutes = 10 };

    void Because() => _valid = _options.IsValid;

    [Fact] void should_be_valid() => _valid.ShouldBeTrue();
}
