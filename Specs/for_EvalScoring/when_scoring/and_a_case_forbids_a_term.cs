// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Evaluation;
using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_EvalScoring.when_scoring;

public class and_a_case_forbids_a_term : Specification
{
    static readonly EvalCase _case = new() { Id = "gen", Expected = "answer", ExpectedPages = ["https://cratis.io/arc/"], MustNotMention = ["Hedging"] };
    static readonly Answer _answer = new("Arc does CQRS [1]", [new("https://cratis.io/arc/")], 0.9, false, [new(new("https://cratis.io/arc/"), "Arc", "Arc", "CQRS", 1, 0.9)]);

    [Fact] void should_pass_an_answer_without_the_term() => EvalScoring.Score(_case, _answer).Passed.ShouldBeTrue();
    [Fact] void should_fail_an_answer_with_the_term_in_any_case() => EvalScoring.Score(_case, _answer with { Text = "Some hedging here [1]" }).Passed.ShouldBeFalse();
}
