// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Cli;

namespace Cratis.Prompter.Specs.for_AskOutput.when_noting;

public class and_citations_carry_provenance : Specification
{
    IReadOnlyList<string> _lines = null!;

    void Because()
    {
        PageUrl page = "https://cratis.io/a.md";
        _lines = AskOutput.Lines(new Answer("Body.", [page], 0.9, false, []) { CitationProvenance = [new(page, new DateTimeOffset(2026, 10, 30, 0, 0, 0, TimeSpan.Zero), null, "16.0.0")] }, verbose: false);
    }

    [Fact] void should_note_the_source_line() => _lines[1].ShouldEqual("  - https://cratis.io/a.md — indexed 2026-10-30 · v16.0.0");
    [Fact] void should_keep_the_answer_text() => _lines[0].ShouldEqual("Body.");
}
