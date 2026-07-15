// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_DiscordAnswers.when_splitting;

public class and_a_hard_split_lands_mid_emoji : Specification
{
    const string Emoji = "\U0001F600";
    IReadOnlyList<string> _messages = null!;

    void Because()
    {
        // A grinning face is a single code point spanning two UTF-16 code units. Placing it so the pair straddles
        // the 2000-code-unit hard-split boundary means a naive fixed-length cut would orphan one of its surrogates.
        _messages = DiscordAnswers.Split(new($"{new string('x', 1999)}{Emoji}{new string('y', 1000)}", [], 0.5, IsRefusal: false, []));
    }

    [Fact] void should_hard_split_into_multiple_messages() => _messages.Count.ShouldBeGreaterThan(1);
    [Fact] void should_keep_every_message_within_the_limit() => _messages.All(message => message.Length <= 2000).ShouldBeTrue();
    [Fact] void should_never_end_a_message_on_a_lone_high_surrogate() => _messages.All(message => !char.IsHighSurrogate(message[^1])).ShouldBeTrue();
    [Fact] void should_keep_every_message_valid_utf16() => _messages.All(message => Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(message)) == message).ShouldBeTrue();
    [Fact] void should_preserve_the_whole_emoji_across_the_split() => string.Concat(_messages).ShouldContain(Emoji);
}
