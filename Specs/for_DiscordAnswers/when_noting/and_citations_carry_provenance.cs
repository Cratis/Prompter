// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Discord;

namespace Cratis.Prompter.Specs.for_DiscordAnswers.when_noting;

public class and_citations_carry_provenance : Specification
{
    Answer _answer = null!;
    IReadOnlyList<string> _chunks = null!;

    void Establish()
    {
        PageUrl[] pages = ["https://cratis.io/a.md", "https://cratis.io/b.md"];
        _answer = new("Body.", pages, 0.9, false, [])
        {
            CitationProvenance = [new(pages[0], new DateTimeOffset(2026, 10, 30, 0, 0, 0, TimeSpan.Zero), null, "16.0.0"), new(pages[1], null, null, null)]
        };
    }

    void Because() => _chunks = DiscordAnswers.Split(_answer);

    [Fact] void should_note_each_source() => _chunks[^1].ShouldContain("<https://cratis.io/a> (indexed 2026-10-30 · v16.0.0) · <https://cratis.io/b> (index date unknown)");
    [Fact] void should_keep_the_original_link_form() => _chunks[^1].ShouldContain("<https://cratis.io/a>");
    [Fact] void should_not_note_a_refusal() => DiscordAnswers.Format(Answer.Refusal(0.1, [])).ShouldNotContain("indexed");
    [Fact] void should_keep_sources_on_the_last_message_when_long() => DiscordAnswers.Split(_answer with { Text = string.Join("\n\n", Enumerable.Repeat(new string('x', 900), 6)) })[^1].ShouldContain("(indexed 2026-10-30 · v16.0.0)");
    [Fact] void should_stay_within_the_limit_when_long() => DiscordAnswers.Split(_answer with { Text = string.Join("\n\n", Enumerable.Repeat(new string('x', 900), 6)) }).All(chunk => chunk.Length <= 2000).ShouldBeTrue();
}
