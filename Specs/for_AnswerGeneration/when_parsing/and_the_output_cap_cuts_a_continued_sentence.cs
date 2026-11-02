// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_AnswerGeneration.when_parsing;

public class and_the_output_cap_cuts_a_continued_sentence : Specification
{
    [Fact] void should_not_keep_the_first_half_of_the_sentence() => AnswerGeneration.Parse("{\"text\":\"- Chronicle stores events and\\n  builds read mod").ShouldBeNull();

    [Fact]
    void should_keep_the_items_before_the_continued_sentence() => AnswerGeneration.Parse("{\"text\":\"- Arc handles commands [2].\\n- Chronicle stores events and\\n  builds read mod")!.Text
        .ShouldEqual("- Arc handles commands [2].\n…");

    [Fact]
    void should_keep_a_sentence_completed_on_the_next_line() => AnswerGeneration.Parse("{\"text\":\"- Chronicle stores events and\\n  builds read models [1].\\n- Arc")!.Text
        .ShouldEqual("- Chronicle stores events and\n  builds read models [1].\n…");
}
