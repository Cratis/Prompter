// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_DiscordAnswers.when_splitting;

public class and_a_code_block_exceeds_the_limit : Specification
{
    IReadOnlyList<string> _messages = null!;

    void Because() => _messages = DiscordAnswers.Split(new(
        $"```csharp\n{string.Join('\n', Enumerable.Repeat("var value = compute();", 120))}\n```",
        [],
        0.5,
        IsRefusal: false,
        []));

    [Fact] void should_hard_split_into_multiple_messages() => _messages.Count.ShouldBeGreaterThan(1);
    [Fact] void should_keep_every_message_within_the_limit() => _messages.All(message => message.Length <= 2000).ShouldBeTrue();
    [Fact] void should_reopen_each_message_with_the_language_hint() => _messages.All(message => message.StartsWith("```csharp", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_balance_the_fences_in_every_message() => _messages.All(message => (message.Split("```").Length - 1) % 2 == 0).ShouldBeTrue();
    [Fact] void should_preserve_all_code_lines_across_the_split() => (string.Concat(_messages).Split("var value = compute();").Length - 1).ShouldEqual(120);
}
