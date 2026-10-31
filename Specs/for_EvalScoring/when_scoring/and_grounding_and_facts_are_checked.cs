// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Evaluation;
using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_EvalScoring.when_scoring;

public class and_grounding_and_facts_are_checked : Specification
{
    static readonly EvalCase _case = new() { Id = "arc", Expected = "answer", ExpectedPages = ["https://cratis.io/arc/"], MustMention = ["CQRS"] };
    static readonly Answer _answer = new("cqrs", [new("https://CRATIS.io/arc/index.md")], 0.9, false, [new(new("https://cratis.io/arc.md"), "Arc", "Arc", "CQRS", 1, 0.9)]);

    [Fact] void should_pass_a_grounded_answer_with_facts() => EvalScoring.Score(_case, _answer).Passed.ShouldBeTrue();
    [Fact] void should_fail_a_missing_citation() => EvalScoring.Score(_case, _answer with { Citations = [] }).Passed.ShouldBeFalse();
    [Fact] void should_fail_a_missing_fact() => EvalScoring.Score(_case, _answer with { Text = "other" }).Passed.ShouldBeFalse();
    [Fact] void should_pass_a_correct_refusal() => EvalScoring.Score(_case with { Expected = "refuse" }, Answer.Refusal(0, [])).Passed.ShouldBeTrue();
    [Fact] void should_fail_a_wrong_refusal() => EvalScoring.Score(_case, Answer.Refusal(0, [])).Passed.ShouldBeFalse();
    [Fact] void should_record_retrieval_independently() => EvalScoring.Score(_case, _answer with { Citations = [] }).RetrievalHit.ShouldBeTrue();
    [Fact] void should_report_the_supplied_cost() => EvalScoring.Score(_case, _answer, 0.1m).Cost.ShouldEqual(0.1m);
}
