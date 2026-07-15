// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Cli;
using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_AskOutput.when_rendering;

public class and_the_answer_is_grounded : Specification
{
    Answer _answer = null!;
    IReadOnlyList<string> _lines = null!;

    void Establish() => _answer = new(
        "Append events through the event log.",
        [new PageUrl("https://cratis.io/chronicle/events.md")],
        0.83,
        IsRefusal: false,
        [new Passage("https://cratis.io/chronicle/events.md", "Events", "Appending events", "content", 0.83)]);

    void Because() => _lines = AskOutput.Lines(_answer, verbose: false);

    [Fact] void should_start_with_the_answer_text() => _lines[0].ShouldEqual("Append events through the event log.");
    [Fact] void should_list_the_citation() => string.Join('\n', _lines).ShouldContain("  - https://cratis.io/chronicle/events.md");
    [Fact] void should_not_list_passages() => string.Join('\n', _lines).ShouldNotContain("Retrieved");
    [Fact] void should_exit_zero() => AskOutput.ExitCode(_answer).ShouldEqual(0);
}
