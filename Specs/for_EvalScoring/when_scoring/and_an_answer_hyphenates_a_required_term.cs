// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Evaluation;
using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_EvalScoring.when_scoring;

public class and_an_answer_hyphenates_a_required_term : Specification
{
    static readonly EvalCase _case = new() { Id = "gen", Expected = "answer", ExpectedPages = ["https://cratis.io/"], MustMention = ["event sourcing"] };
    static readonly Answer _answer = new("Cratis is an event-sourcing platform [1]", [new("https://cratis.io/")], 0.9, false, [new(new("https://cratis.io/"), "Cratis", "Cratis", "Events", 1, 0.9)]);

    [Fact] void should_count_the_hyphenated_form_as_a_mention() => EvalScoring.Score(_case, _answer).MustMentionHit.ShouldBeTrue();
    [Fact] void should_count_a_line_break_as_a_space() => EvalScoring.Score(_case, _answer with { Text = "Event\nsourcing at heart [1]" }).MustMentionHit.ShouldBeTrue();
    [Fact] void should_not_count_a_different_term() => EvalScoring.Score(_case, _answer with { Text = "Cratis stores events [1]" }).MustMentionHit.ShouldBeFalse();
}
