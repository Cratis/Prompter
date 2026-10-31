// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Cli;

namespace Cratis.Prompter.Specs.for_EvalArguments.when_parsing;

public class and_flags_are_supplied : Specification
{
    [Theory]
    [InlineData("--unknown", "value")]
    [InlineData("--limit", "0")]
    [InlineData("--limit", "no")]
    [InlineData("--max-cost", "-1")]
    [InlineData("--min-pass-rate", "NaN")]
    [InlineData("--min-pass-rate", "1.1")]
    [InlineData("--min-retrieval-hit-rate", "-0.1")]
    [InlineData("--min-refusal-accuracy", "Infinity")]
    [InlineData("--set", "--paid")]
    void should_reject_invalid_values(string flag, string value) => (EvalArguments.Parse([flag, value]) is null).ShouldBeTrue();

    [Fact] void should_require_a_value() => (EvalArguments.Parse(["--limit"]) is null).ShouldBeTrue();
    [Fact] void should_reject_repeated_flags() => (EvalArguments.Parse(["--paid", "--paid"]) is null).ShouldBeTrue();
    [Fact] void should_default_to_unpaid() => EvalArguments.Parse([])!.Paid.ShouldBeFalse();
    [Fact] void should_keep_the_default_cost_cap() => EvalArguments.Parse([])!.MaxCost.ShouldEqual(2m);
    [Fact] void should_parse_explicit_options() => EvalArguments.Parse(["--paid", "--set", "cases.yaml", "--case", "arc-001", "--limit", "2", "--max-cost", "0.5", "--out", "report.json", "--min-pass-rate", "0.7", "--min-retrieval-hit-rate", "0.6", "--min-refusal-accuracy", "0.8"])
        .ShouldEqual(new EvalArguments(true, "cases.yaml", "arc-001", 2, 0.7, 0.6, 0.8, 0.5m, "report.json"));
}
