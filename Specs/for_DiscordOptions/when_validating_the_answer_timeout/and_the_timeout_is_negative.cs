// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_DiscordOptions.when_validating_the_answer_timeout;

public class and_the_timeout_is_negative : Specification
{
    DiscordOptions _options = null!;
    bool _valid;

    void Establish() => _options = new DiscordOptions { AnswerTimeoutSeconds = -5 };

    void Because() => _valid = _options.AnswerTimeoutIsValid;

    [Fact] void should_be_invalid() => _valid.ShouldBeFalse();
}
