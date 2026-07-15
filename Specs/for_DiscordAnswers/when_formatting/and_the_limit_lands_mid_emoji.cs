// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_DiscordAnswers.when_formatting;

public class and_the_limit_lands_mid_emoji : Specification
{
    const string Emoji = "\U0001F600";
    string _result = null!;

    void Because()
    {
        // A grinning face is a single code point spanning two UTF-16 code units (a surrogate pair). Placing it so
        // the pair straddles the truncation boundary means a naive fixed-length cut would keep only its high surrogate.
        _result = DiscordAnswers.Format(new($"{new string('x', 1998)}{Emoji}{new string('y', 100)}", [], 0.5, IsRefusal: false, []));
    }

    [Fact] void should_cap_at_discord_message_length() => (_result.Length <= 2000).ShouldBeTrue();
    [Fact] void should_end_with_ellipsis() => _result.EndsWith('…').ShouldBeTrue();
    [Fact] void should_not_leave_a_lone_high_surrogate_before_the_ellipsis() => char.IsHighSurrogate(_result[^2]).ShouldBeFalse();
    [Fact] void should_stay_valid_utf16() => Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(_result)).ShouldEqual(_result);
}
