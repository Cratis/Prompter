// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_Answers.when_applying_the_retrieval_gate;

public class and_similarity_is_at_the_boundary : given.a_grounded_answerer
{
    Answer _below = null!;

    async Task Because()
    {
        Found(Passage(0.5));
        result = await answers.For(new("What is Cratis Chronicle?"), "cli");
        Found(Passage(0.5 - 1e-6), Passage(-0.3), Passage(0));
        _below = await answers.For(new("What is Cratis Chronicle?"), "cli");
    }

    [Fact] void should_admit_the_boundary() => result.IsRefusal.ShouldBeFalse();
    [Fact] void should_report_the_boundary_similarity() => result.Confidence.ShouldEqual(0.5);
    [Fact] void should_refuse_just_below_the_boundary() => _below.IsRefusal.ShouldBeTrue();
    [Fact] void should_report_the_best_valid_weak_candidate() => _below.Confidence.ShouldEqual(0.5 - 1e-6);
}
