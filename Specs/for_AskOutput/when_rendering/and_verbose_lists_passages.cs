// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Cli;
using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_AskOutput.when_rendering;

public class and_verbose_lists_passages : Specification
{
    Answer _answer = null!;
    IReadOnlyList<string> _lines = null!;

    void Establish() => _answer = new(
        "Append events through the event log.",
        [new PageUrl("https://cratis.io/chronicle/events.md")],
        0.83,
        IsRefusal: false,
        [
            new Passage("https://cratis.io/chronicle/events.md", "Events", "Appending events", "content", 0.83),
            new Passage("https://cratis.io/chronicle/concepts.md", "Concepts", "Event Store", "content", 0.79)
        ]);

    void Because() => _lines = AskOutput.Lines(_answer, verbose: true);

    [Fact] void should_report_the_passage_count_and_top_score() => _lines[0].ShouldEqual("Retrieved 2 passages, top score 0.830:");
    [Fact] void should_list_the_first_passage_with_score_page_and_heading() => string.Join('\n', _lines).ShouldContain("  [1] 0.830  https://cratis.io/chronicle/events.md — Appending events");
    [Fact] void should_list_the_second_passage() => string.Join('\n', _lines).ShouldContain("  [2] 0.790  https://cratis.io/chronicle/concepts.md — Event Store");
    [Fact] void should_still_include_the_answer_text() => string.Join('\n', _lines).ShouldContain("Append events through the event log.");
    [Fact] void should_exit_zero() => AskOutput.ExitCode(_answer).ShouldEqual(0);
}
