// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_PrompterOptions.when_validating_retention;

public class and_retention_is_negative : Specification
{
    PrompterOptions _options = null!;
    bool _valid;

    void Establish() => _options = new PrompterOptions { RetentionDays = -1 };

    void Because() => _valid = _options.RetentionIsValid;

    [Fact] void should_be_invalid() => _valid.ShouldBeFalse();
}
