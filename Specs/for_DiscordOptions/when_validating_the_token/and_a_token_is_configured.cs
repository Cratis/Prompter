// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_DiscordOptions.when_validating_the_token;

public class and_a_token_is_configured : Specification
{
    DiscordOptions _options = null!;
    bool _present;

    void Establish() => _options = new DiscordOptions { Token = "a-bot-token" };

    void Because() => _present = _options.TokenIsPresent;

    [Fact] void should_be_present() => _present.ShouldBeTrue();
}
