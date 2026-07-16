// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_VoyageOptions.when_validating_dimensions;

public class and_the_configuration_is_default : Specification
{
    VoyageOptions _options = null!;
    bool _matches;

    void Establish() => _options = new VoyageOptions();

    void Because() => _matches = _options.DimensionsMatchSchema;

    [Fact] void should_match_the_schema() => _matches.ShouldBeTrue();
}
