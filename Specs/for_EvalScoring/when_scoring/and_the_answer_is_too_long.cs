// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Evaluation;

namespace Cratis.Prompter.Specs.for_EvalScoring.when_scoring;

public class and_the_answer_is_too_long : Specification
{
    static readonly EvalCase _case = new() { Id = "gen-001", Expected = "answer", ExpectedPages = ["https://cratis.io/"] };
    EvalResult _result = null!;

    void Because() => _result = EvalScoring.Score(_case, new(new string('a', 10_000), [new("https://cratis.io/")], 0.9, false, []));

    [Fact] void should_bound_the_text() => _result.AnswerText!.Length.ShouldEqual(EvalScoring.MaxAnswerTextLength);
    [Fact] void should_end_with_the_marker() => _result.AnswerText!.EndsWith(EvalScoring.TruncationMarker, StringComparison.Ordinal).ShouldBeTrue();
    [Fact]
    void should_keep_text_at_the_limit_whole() =>
        EvalScoring.Score(_case, new(new string('a', EvalScoring.MaxAnswerTextLength), [new("https://cratis.io/")], 0.9, false, [])).AnswerText!.Length.ShouldEqual(EvalScoring.MaxAnswerTextLength);
    [Fact]
    void should_not_split_a_surrogate_pair() =>
        char.IsHighSurrogate(EvalScoring.Score(_case, new(new string('a', EvalScoring.MaxAnswerTextLength - EvalScoring.TruncationMarker.Length - 1) + "😀" + new string('b', 100), [new("https://cratis.io/")], 0.9, false, [])).AnswerText![^(EvalScoring.TruncationMarker.Length + 1)]).ShouldBeFalse();
}
