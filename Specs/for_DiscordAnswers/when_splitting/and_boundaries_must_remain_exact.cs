// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_DiscordAnswers.when_splitting;

/// <summary>Preserves exact paragraph, fence, and oversized language-hint boundaries.</summary>
public class and_boundaries_must_remain_exact : Specification
{
    IReadOnlyList<string> _paragraphs = null!;
    IReadOnlyList<string> _unclosed = null!;
    IReadOnlyList<string> _longHint = null!;

    static IReadOnlyList<string> Split(string text) => DiscordAnswers.Split(new(text, [], 0.5, false, []));

    void Because()
    {
        _paragraphs = Split(new string('a', 1000) + "\r\n \r\n" + new string('b', 1000) + "\r\r```cs\rx\r\rz\r```");
        _unclosed = Split("```csharp\n" + new string('a', 2000));
        _longHint = Split("```" + new string('x', 1997) + "\nz\n```");
    }

    [Fact] void should_normalize_paragraphs_without_changing_code_blank_lines() => _paragraphs.ShouldEqual<string>([new string('a', 1000), new string('b', 1000) + "\n\n```cs\nx\n\nz\n```"]);
    [Fact] void should_close_and_reopen_an_unclosed_wrapped_block() => _unclosed.ShouldEqual<string>(["```csharp\n" + new string('a', 1986) + "\n```", "```csharp\n" + new string('a', 14) + "\n```"]);
    [Fact] void should_fall_back_to_text_splitting_for_an_oversized_hint() => _longHint.ShouldEqual<string>(["```" + new string('x', 1997), "\nz\n```"]);
}
