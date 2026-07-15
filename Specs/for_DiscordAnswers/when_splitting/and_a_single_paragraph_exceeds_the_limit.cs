// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_DiscordAnswers.when_splitting;

public class and_a_single_paragraph_exceeds_the_limit : Specification
{
    IReadOnlyList<string> _messages = null!;

    void Because() => _messages = DiscordAnswers.Split(new(new string('x', 3000), [], 0.5, IsRefusal: false, []));

    [Fact] void should_hard_split_into_multiple_messages() => _messages.Count.ShouldBeGreaterThan(1);
    [Fact] void should_keep_every_message_within_the_limit() => _messages.All(message => message.Length <= 2000).ShouldBeTrue();
    [Fact] void should_preserve_the_whole_paragraph_across_messages() => string.Concat(_messages).ShouldContain(new string('x', 3000));
}
