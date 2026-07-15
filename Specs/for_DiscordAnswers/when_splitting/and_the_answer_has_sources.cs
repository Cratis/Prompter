// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_DiscordAnswers.when_splitting;

public class and_the_answer_has_sources : Specification
{
    IReadOnlyList<string> _messages = null!;

    void Because() => _messages = DiscordAnswers.Split(new(
        string.Join("\n\n", new string('a', 1500), new string('b', 1500)),
        [new PageUrl("https://cratis.io/chronicle/events.md")],
        0.5,
        IsRefusal: false,
        []));

    [Fact] void should_split_into_two_messages() => _messages.Count.ShouldEqual(2);
    [Fact] void should_land_sources_on_the_last_message() => _messages[^1].ShouldContain("Sources: <https://cratis.io/chronicle/events>");
    [Fact] void should_not_repeat_sources_on_earlier_messages() => _messages[0].ShouldNotContain("Sources:");
    [Fact] void should_keep_every_message_within_the_limit() => _messages.All(message => message.Length <= 2000).ShouldBeTrue();
}
