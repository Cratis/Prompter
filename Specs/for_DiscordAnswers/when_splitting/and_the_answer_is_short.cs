// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_DiscordAnswers.when_splitting;

public class and_the_answer_is_short : Specification
{
    Answer _answer = null!;
    IReadOnlyList<string> _messages = null!;

    void Establish() => _answer = new(
        "Append events through the event log.",
        [new PageUrl("https://cratis.io/chronicle/events.md")],
        0.5,
        IsRefusal: false,
        []);

    void Because() => _messages = DiscordAnswers.Split(_answer);

    [Fact] void should_produce_a_single_message() => _messages.Count.ShouldEqual(1);
    [Fact] void should_be_identical_to_the_single_message_format() => _messages[0].ShouldEqual(DiscordAnswers.Format(_answer));
}
