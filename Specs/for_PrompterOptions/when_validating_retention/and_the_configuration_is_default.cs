// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_PrompterOptions.when_validating_retention;

public class and_the_configuration_is_default : Specification
{
    PrompterOptions _options = null!;
    bool _valid;

    void Establish() => _options = new PrompterOptions();

    void Because() => _valid = _options.RetentionIsValid;

    [Fact] void should_be_valid() => _valid.ShouldBeTrue();
}
