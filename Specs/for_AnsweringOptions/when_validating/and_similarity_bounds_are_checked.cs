// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_AnsweringOptions.when_validating;

public class and_similarity_bounds_are_checked : Specification
{
    bool[] _valid = [];
    bool[] _invalid = [];
    AnsweringOptions _defaults = null!;

    void Because()
    {
        _defaults = new();
        _valid = new[] { double.Epsilon, 0.5, 1.0 }.Select(value => new AnsweringOptions { MinSemanticSimilarity = value }.SimilarityIsValid).ToArray();
        _invalid = new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity, -1, 0, 1.000001 }
            .Select(value => new AnsweringOptions { MinSemanticSimilarity = value }.SimilarityIsValid).ToArray();
    }

    [Fact] void should_default_to_the_initial_policy() => _defaults.MinSemanticSimilarity.ShouldEqual(0.5);
    [Fact] void should_accept_finite_positive_cosine_cutoffs() => _valid.All(value => value).ShouldBeTrue();
    [Fact] void should_reject_nonfinite_nonpositive_and_out_of_domain_cutoffs() => _invalid.Any(value => value).ShouldBeFalse();
}
