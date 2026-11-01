// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Cli;
using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_AskOutput.when_rendering;

public class and_only_a_later_page_is_cited : Specification
{
    IReadOnlyList<string> _lines = null!;

    void Because()
    {
        var answer = new Answer(
            "Only B [1].",
            [new PageUrl("https://cratis.io/b.md")],
            0.8,
            false,
            [
                new Passage("https://cratis.io/a.md", "A", "Intro", "A content", 0.02, 0.8),
                new Passage("https://cratis.io/a.md", "A", "Details", "More A content", 0.01, 0.7),
                new Passage("https://cratis.io/b.md", "B", "Overview", "B content", 0.01, 0.6)
            ]);
        _lines = AskOutput.Lines(answer, verbose: true);
    }

    [Fact] void should_number_the_final_sources_not_the_passages() => _lines[^1].ShouldEqual("  [1] https://cratis.io/b.md");
    [Fact] void should_not_label_an_uncited_passage_as_source_one() => _lines[2].StartsWith("  passage 1: rrf", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_the_normalized_answer_marker() => _lines[^2].ShouldEqual("Only B [1].");
}
