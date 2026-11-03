// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Evaluation;

namespace Cratis.Prompter.Specs.for_EvalScoring.when_scoring;

public class and_terms_are_missed : Specification
{
    static readonly EvalCase _case = new() { Id = "gen", Expected = "answer", ExpectedPages = ["https://cratis.io/"], MustMention = ["Chronicle", "Arc", "Components"], MustNotMention = ["Hedging"] };
    static readonly Answer _answer = new("Chronicle stores events; the excerpts say little, hedging [1]", [new("https://cratis.io/")], 0.9, false, []);
    EvalResult _result = null!;

    void Because() => _result = EvalScoring.Score(_case, _answer);

    [Fact] void should_list_each_missing_required_term() => _result.TermMisses.ShouldContainOnly("Arc", "Components", "!excerpt", "!Hedging");
    [Fact] void should_not_list_generated_text() => _result.TermMisses.ShouldNotContain("Chronicle stores events; the excerpts say little, hedging [1]");
    [Fact] void should_list_nothing_when_every_term_is_met() => EvalScoring.Score(_case, _answer with { Text = "Chronicle, Arc and Components [1]" }).TermMisses.ShouldBeEmpty();
}
