// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Cli;
using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_AskOutput.when_rendering;

public class and_verbose_shows_the_refusal_score : Specification
{
    Answer _answer = null!;
    IReadOnlyList<string> _lines = null!;

    void Establish() => _answer = Answer.Refusal(
        0.21,
        [new Passage("https://cratis.io/some/page.md", "Some", "Heading", "content", 0.21)]);

    void Because() => _lines = AskOutput.Lines(_answer, verbose: true);

    [Fact] void should_show_the_top_score_that_fell_below_threshold() => _lines[0].ShouldEqual("Retrieved 1 passage, top score 0.210:");
    [Fact] void should_use_the_singular_for_one_passage() => _lines[0].ShouldContain("1 passage,");
    [Fact] void should_exit_non_zero() => AskOutput.ExitCode(_answer).ShouldEqual(1);
}
