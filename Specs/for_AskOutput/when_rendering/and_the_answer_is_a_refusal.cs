// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Cli;
using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_AskOutput.when_rendering;

public class and_the_answer_is_a_refusal : Specification
{
    Answer _answer = null!;
    IReadOnlyList<string> _lines = null!;

    void Establish() => _answer = Answer.Refusal(
        0.21,
        [new Passage("https://cratis.io/some/page.md", "Some", "Heading", "content", 0.21)]);

    void Because() => _lines = AskOutput.Lines(_answer, verbose: false);

    [Fact] void should_exit_non_zero() => AskOutput.ExitCode(_answer).ShouldEqual(1);
    [Fact] void should_show_the_refusal_text() => _lines[0].ShouldContain("couldn't find anything");
    [Fact] void should_have_no_citation_lines() => string.Join('\n', _lines).ShouldNotContain("  - ");
}
