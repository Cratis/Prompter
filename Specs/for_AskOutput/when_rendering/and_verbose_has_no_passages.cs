// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Cli;

namespace Cratis.Prompter.Specs.for_AskOutput.when_rendering;

public class and_verbose_has_no_passages : Specification
{
    Answer _answer = null!;
    IReadOnlyList<string> _lines = null!;

    void Establish() => _answer = Answer.Refusal(0, []);

    void Because() => _lines = AskOutput.Lines(_answer, verbose: true);

    [Fact] void should_report_zero_passages() => _lines[0].ShouldEqual("Retrieved 0 passages.");
    [Fact] void should_still_show_the_answer() => string.Join('\n', _lines).ShouldContain("couldn't find anything");
}
