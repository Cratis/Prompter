// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Cli;
using Cratis.Prompter.Evaluation;

namespace Cratis.Prompter.Specs.for_EvalSummary.when_summarizing;

public class and_thresholds_and_caps_apply : Specification
{
    static readonly EvalResult _passed = new("safe", true, true, true, true, true, true, 0.1m, []);
    static readonly EvalArguments _arguments = new(true);

    [Fact] void should_pass_at_all_thresholds() => EvalSummary.From([_passed], _arguments).ExitCode.ShouldEqual(0);
    [Fact] void should_fail_below_the_pass_threshold() => EvalSummary.From([_passed with { Passed = false }], _arguments).ExitCode.ShouldEqual(1);
    [Fact] void should_fail_below_the_retrieval_threshold() => EvalSummary.From([_passed with { RetrievalHit = false }], _arguments).ExitCode.ShouldEqual(1);
    [Fact] void should_fail_below_the_refusal_threshold() => EvalSummary.From([_passed with { RefusalCorrect = false }], _arguments).ExitCode.ShouldEqual(1);
    [Fact] void should_fail_an_empty_set() => EvalSummary.From([], _arguments).ExitCode.ShouldEqual(1);
    [Fact] void should_fail_a_cost_cap_stop() => EvalSummary.From([_passed], _arguments, true).ExitCode.ShouldEqual(1);
    [Fact] void should_fail_an_overspend() => EvalSummary.From([_passed with { Cost = 3m }], _arguments).ExitCode.ShouldEqual(1);
    [Fact] void should_sum_spend() => EvalSummary.From([_passed, _passed], _arguments).TotalCost.ShouldEqual(0.2m);
    [Fact] void should_count_refusal_accuracy_over_all_cases() => EvalSummary.From([_passed, _passed with { ExpectsAnswer = false, RefusalCorrect = false }], _arguments).RefusalAccuracy.ShouldEqual(0.5);
}
