// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_DiscordAnswers.when_advising;

public class and_secrets_were_removed : Specification
{
    string _advice = string.Empty;
    string _harmless = string.Empty;

    void Because()
    {
        var answer = Answer.Refusal(0, []);
        _advice = DiscordAnswers.Advice(new(answer with { SecretsRedacted = true }, [], "existing offer"));
        _harmless = DiscordAnswers.Advice(new(answer, [], "existing offer"));
    }

    [Fact] void should_start_with_the_fixed_notice() => _advice.StartsWith(DiscordAnswers.SecretNotice, StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_preserve_existing_advice() => _advice.EndsWith("existing offer", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_leave_harmless_advice_unchanged() => _harmless.ShouldEqual("existing offer");
}
