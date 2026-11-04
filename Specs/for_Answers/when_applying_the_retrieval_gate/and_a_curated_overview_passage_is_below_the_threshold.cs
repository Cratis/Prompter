// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_Answers.when_applying_the_retrieval_gate;

public class and_a_curated_overview_passage_is_below_the_threshold : given.a_grounded_answerer
{
    Passage _overview = null!;

    void Establish()
    {
        _overview = Passage(0.1) with { IsCuratedOverview = true, Page = new("https://cratis.io/why-cratis/"), Content = "Why developers choose Cratis." };
        Found(_overview);
    }

    async Task Because() => result = await answers.For(new("What is Cratis good at?"), "cli");

    [Fact] void should_admit_it_despite_the_low_similarity() => result.IsRefusal.ShouldBeFalse();
    [Fact] void should_cite_the_curated_overview_page() => result.Citations.SequenceEqual([_overview.Page]).ShouldBeTrue();
    [Fact] void should_include_its_excerpt() => sent[1].Text.ShouldContain("Why developers choose Cratis.");
    [Fact] void should_report_its_similarity_as_the_confidence() => result.Confidence.ShouldEqual(0.1);
}
