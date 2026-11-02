// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_AnswerGeneration.when_parsing;

public class and_the_output_cap_cuts_a_list : Specification
{
    const string Cut = "{\"text\":\"- **Chronicle**: \\\"event store\\\" [1]\\n- **Arc**: CQRS \\u2014 typed [1]\\n- **Comp";

    GeneratedAnswer _result = null!;

    void Because() => _result = AnswerGeneration.Parse(Cut)!;

    [Fact] void should_keep_the_complete_lines_and_mark_the_answer_as_shortened() => _result.Text.ShouldEqual("- **Chronicle**: \"event store\" [1]\n- **Arc**: CQRS \u2014 typed [1]\n\u2026");
    [Fact] void should_classify_it_as_unknown() => _result.Category.ShouldEqual(InteractionCategory.Unknown);
    [Fact] void should_reject_a_cut_inside_an_escape_before_any_line() => AnswerGeneration.Parse("{\"text\":\"Chronicle \\").ShouldBeNull();
    [Fact] void should_reject_a_cut_that_leaves_only_blank_lines() => AnswerGeneration.Parse("{\"text\":\"  \\n\\nChron").ShouldBeNull();
    [Fact] void should_not_treat_an_escaped_backslash_as_a_line_break() => AnswerGeneration.Parse("{\"text\":\"C:\\\\new folder").ShouldBeNull();
    [Fact] void should_reject_a_cut_list_whose_first_property_is_not_text() => AnswerGeneration.Parse("{\"category\":\"answered\",\"text\":\"- Arc [1]\\n- Chr").ShouldBeNull();
}
