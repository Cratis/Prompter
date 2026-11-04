// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Evaluation;

namespace Cratis.Prompter.Specs.for_EvalScoring.when_scoring;

public class and_the_answer_is_recorded : Specification
{
    static readonly EvalCase _case = new() { Id = "gen-001", Expected = "answer", ExpectedPages = ["https://cratis.io/"] };
    static readonly Answer _answer = new("Chronicle stores events [1]", [new("https://cratis.io/")], 0.9, false, []) { Category = InteractionCategory.Answered };
    EvalResult _result = null!;

    void Because() => _result = EvalScoring.Score(_case, _answer);

    [Fact] void should_carry_the_answer_text() => _result.AnswerText.ShouldEqual("Chronicle stores events [1]");
    [Fact] void should_carry_the_category() => _result.Category.ShouldEqual("Answered");
    [Fact] void should_carry_that_it_was_not_a_refusal() => _result.IsRefusal.ShouldBeFalse();
    [Fact] void should_carry_that_a_refusal_was_a_refusal() => EvalScoring.Score(_case, _answer with { IsRefusal = true }).IsRefusal.ShouldBeTrue();
}
