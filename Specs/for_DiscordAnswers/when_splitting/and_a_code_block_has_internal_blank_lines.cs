// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_DiscordAnswers.when_splitting;

public class and_a_code_block_has_internal_blank_lines : Specification
{
    IReadOnlyList<string> _messages = null!;

    void Because() => _messages = DiscordAnswers.Split(new(
        string.Join("\n\n", new string('a', 1200), new string('b', 1200), "```csharp\nvar a = 1;\n\nvar b = 2;\n```"),
        [],
        0.5,
        IsRefusal: false,
        []));

    [Fact] void should_split_into_multiple_messages() => _messages.Count.ShouldBeGreaterThan(1);
    [Fact] void should_keep_the_code_block_whole_in_a_single_message() =>
        _messages.Count(message => message.Contains("```csharp\nvar a = 1;\n\nvar b = 2;\n```", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_keep_every_message_within_the_limit() => _messages.All(message => message.Length <= 2000).ShouldBeTrue();
}
