// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_DiscordAnswers.when_splitting;

public class and_a_code_line_wraps_mid_emoji : Specification
{
    const string Emoji = "\U0001F600";
    const int EmojiCount = 1000;
    IReadOnlyList<string> _messages = null!;

    void Because()
    {
        // A single over-long code line packed with grinning faces (one code point, two UTF-16 code units each) after
        // one filler char, so an emoji pair straddles the per-line wrap budget and a naive cut would orphan a surrogate.
        _messages = DiscordAnswers.Split(new(
            $"```csharp\nx{string.Concat(Enumerable.Repeat(Emoji, EmojiCount))}\n```",
            [],
            0.5,
            IsRefusal: false,
            []));
    }

    [Fact] void should_wrap_into_multiple_messages() => _messages.Count.ShouldBeGreaterThan(1);
    [Fact] void should_keep_every_message_within_the_limit() => _messages.All(message => message.Length <= 2000).ShouldBeTrue();
    [Fact] void should_never_end_a_message_on_a_lone_high_surrogate() => _messages.All(message => !char.IsHighSurrogate(message[^1])).ShouldBeTrue();
    [Fact] void should_keep_every_message_valid_utf16() => _messages.All(message => Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(message)) == message).ShouldBeTrue();
    [Fact] void should_balance_the_fences_in_every_message() => _messages.All(message => (message.Split("```").Length - 1) % 2 == 0).ShouldBeTrue();
    [Fact] void should_preserve_every_whole_emoji_across_the_wrap() => (string.Concat(_messages).Split(Emoji).Length - 1).ShouldEqual(EmojiCount);
}
