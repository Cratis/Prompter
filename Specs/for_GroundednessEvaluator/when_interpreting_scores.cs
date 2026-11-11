// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Eval.Scoring;
using Microsoft.Extensions.AI.Evaluation;

namespace Cratis.Prompter.Specs.for_GroundednessEvaluator;

public class when_interpreting_scores : Specification
{
    EvaluationMetricInterpretation[] _results = null!;

    void Because() => _results = new double?[] { 5, 4, 3, 2, 1, null, 0, 6, 4.5, double.NaN }
        .Select(score => GroundednessEvaluator.Interpret(new NumericMetric(GroundednessEvaluator.MetricName) { Value = score })).ToArray();

    [Fact] void should_distinguish_valid_scores() => _results.Take(5).Select(result => result.Rating).ShouldEqual([EvaluationRating.Exceptional, EvaluationRating.Good, EvaluationRating.Average, EvaluationRating.Poor, EvaluationRating.Unacceptable]);
    [Fact] void should_fail_only_poor_or_unacceptable_valid_scores() => _results.Take(5).Select(result => result.Failed).ShouldEqual([false, false, false, true, true]);
    [Fact] void should_mark_invalid_or_missing_scores_inconclusive() => _results.Skip(5).Select(result => result.Rating).ShouldEqual(Enumerable.Repeat(EvaluationRating.Inconclusive, 5));
    [Fact] void should_fail_invalid_or_missing_scores() => _results.Skip(5).Select(result => result.Failed).ShouldEqual(Enumerable.Repeat(true, 5));
}
