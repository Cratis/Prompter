// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Cli;

namespace Cratis.Prompter.Specs.for_EvalArguments.when_parsing;

/// <summary>Preserves defaults, numeric boundaries, and malformed-option rejection.</summary>
public class and_option_boundaries_must_remain_exact : Specification
{
    EvalArguments?[] _results = null!;

    void Because() => _results = new string[][]
    {
        [],
        ["--limit", "+2", "--skip", "00", "--max-cost", "0.01", "--out", "-", "--min-pass-rate", "0", "--min-retrieval-hit-rate", "1"],
        ["--set", string.Empty],
        ["--skip", "+1"],
        ["--set", "one", "--set", "two"],
        ["--PAID"],
        ["--out", "--other"],
        ["--paid", "unexpected"],
        ["--min-pass-rate", "1e-1"]
    }.Select(EvalArguments.Parse).ToArray();

    [Fact] void should_preserve_every_default() => _results[0].ShouldEqual(new EvalArguments(false));
    [Fact] void should_preserve_numeric_boundaries_and_stdout() => _results[1].ShouldEqual(new EvalArguments(false, Limit: 2, MinPassRate: 0, MinRetrievalHitRate: 1, MaxCost: 0.01m, Out: "-"));
    [Fact] void should_accept_an_empty_path() => _results[2]!.Set.ShouldEqual(string.Empty);
    [Fact] void should_reject_signed_skips_duplicates_unknown_flags_and_missing_values() => _results.Skip(3).Take(5).ShouldEqual(Enumerable.Repeat<EvalArguments?>(null, 5));
    [Fact] void should_accept_exponential_rates() => _results[8]!.MinPassRate.ShouldEqual(0.1);
}
