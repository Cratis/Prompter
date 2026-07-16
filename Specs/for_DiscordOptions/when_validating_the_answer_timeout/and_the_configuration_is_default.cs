// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_DiscordOptions.when_validating_the_answer_timeout;

public class and_the_configuration_is_default : Specification
{
    DiscordOptions _options = null!;
    bool _valid;

    void Establish() => _options = new DiscordOptions();

    void Because() => _valid = _options.AnswerTimeoutIsValid;

    [Fact] void should_be_valid() => _valid.ShouldBeTrue();
}
