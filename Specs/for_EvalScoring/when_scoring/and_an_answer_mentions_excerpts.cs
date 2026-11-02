// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Evaluation;
using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_EvalScoring.when_scoring;

public class and_an_answer_mentions_excerpts : Specification
{
    static readonly EvalCase _case = new() { Id = "gen", Expected = "answer", ExpectedPages = ["https://cratis.io/arc/"] };
    static readonly Answer _answer = new("The Excerpts don't say [1]", [new("https://cratis.io/arc/")], 0.9, false, [new(new("https://cratis.io/arc/"), "Arc", "Arc", "CQRS", 1, 0.9)]);

    [Fact] void should_not_count_the_required_terms_as_met() => EvalScoring.Score(_case, _answer).MustMentionHit.ShouldBeFalse();
    [Fact] void should_fail_the_case() => EvalScoring.Score(_case, _answer).Passed.ShouldBeFalse();
    [Fact] void should_still_pass_a_correct_refusal() => EvalScoring.Score(_case with { Expected = "refuse" }, Answer.Refusal(0, [])).Passed.ShouldBeTrue();
}
