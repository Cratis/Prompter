// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_Passage.when_checking_the_floor;

public class and_the_passage_is_curated : Specification
{
    static Passage At(double similarity, bool curated) => new(new("https://cratis.io/"), "T", "H", "C", 0, similarity) { IsCurated = curated };

    [Fact] void should_admit_a_curated_passage_below_the_floor() => At(0.3, true).ClearsFloor(0.5).ShouldBeTrue();
    [Fact] void should_refuse_an_ordinary_passage_below_the_floor() => At(0.3, false).ClearsFloor(0.5).ShouldBeFalse();
    [Fact] void should_admit_an_ordinary_passage_at_the_floor() => At(0.5, false).ClearsFloor(0.5).ShouldBeTrue();
    [Fact] void should_refuse_a_curated_passage_without_a_finite_similarity() => At(double.NaN, true).ClearsFloor(0.5).ShouldBeFalse();
    [Fact] void should_refuse_a_curated_passage_with_an_out_of_range_similarity() => At(1.5, true).ClearsFloor(0.5).ShouldBeFalse();
}
