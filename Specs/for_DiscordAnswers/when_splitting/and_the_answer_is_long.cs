// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_DiscordAnswers.when_splitting;

public class and_the_answer_is_long : Specification
{
    IReadOnlyList<string> _messages = null!;

    void Because() => _messages = DiscordAnswers.Split(new(
        string.Join("\n\n", new string('a', 800), new string('b', 800), new string('c', 800), new string('d', 800)),
        [],
        0.5,
        IsRefusal: false,
        []));

    [Fact] void should_produce_more_than_one_message() => _messages.Count.ShouldBeGreaterThan(1);
    [Fact] void should_keep_every_message_within_the_limit() => _messages.All(message => message.Length <= 2000).ShouldBeTrue();
}
