// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_RateLimitOptions.when_validating;

public class and_the_configuration_is_default : Specification
{
    RateLimitOptions _options = null!;
    bool _valid;

    void Establish() => _options = new RateLimitOptions();

    void Because() => _valid = _options.IsValid;

    [Fact] void should_be_valid() => _valid.ShouldBeTrue();
}
