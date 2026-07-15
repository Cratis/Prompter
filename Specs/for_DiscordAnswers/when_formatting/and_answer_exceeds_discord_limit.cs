// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_DiscordAnswers.when_formatting;

public class and_answer_exceeds_discord_limit : Specification
{
    string _result = null!;

    void Because() => _result = DiscordAnswers.Format(new(new string('x', 3000), [], 0.5, IsRefusal: false, []));

    [Fact] void should_cap_at_discord_message_length() => (_result.Length <= 2000).ShouldBeTrue();
    [Fact] void should_end_with_ellipsis() => _result.EndsWith('…').ShouldBeTrue();
}
