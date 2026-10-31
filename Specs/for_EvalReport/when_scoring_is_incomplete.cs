// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Eval;

namespace Cratis.Prompter.Specs.for_EvalReport;

public class when_scoring_is_incomplete : Specification
{
    readonly QuestionResult _answer = new("answer", "arc", "concept", false, false, true, true, 4, 1, [], null);
    readonly QuestionResult _refusal = new("refusal", "other", "refusal", true, true, true, null, null, 1, [], null);

    [Fact] void should_reject_missing_required_samples() => new EvalReport([], DateTimeOffset.UnixEpoch).ExitCode.ShouldEqual(1);
    [Fact] void should_reject_missing_judge_output() => Report(_answer with { Groundedness = null }).ExitCode.ShouldEqual(1);
    [Fact] void should_reject_nonfinite_judge_output() => Report(_answer with { Groundedness = double.NaN }).ExitCode.ShouldEqual(1);
    [Fact] void should_reject_infinite_judge_output() => Report(_answer with { Groundedness = double.PositiveInfinity }).ExitCode.ShouldEqual(1);
    [Fact] void should_reject_out_of_range_judge_output() => Report(_answer with { Groundedness = 6 }).ExitCode.ShouldEqual(1);
    [Fact] void should_reject_errors() => Report(_answer with { Error = "synthetic failure" }).ExitCode.ShouldEqual(1);
    [Fact] void should_accept_complete_low_quality_measurements() => Report(_answer with { Groundedness = 1 }).ExitCode.ShouldEqual(0);

    EvalReport Report(QuestionResult answer) => new([answer, _refusal], DateTimeOffset.UnixEpoch);
}
