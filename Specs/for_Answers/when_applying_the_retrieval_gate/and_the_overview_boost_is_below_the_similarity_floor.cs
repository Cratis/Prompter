// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_Answers.when_applying_the_retrieval_gate;

public class and_the_overview_boost_is_below_the_similarity_floor : given.a_grounded_answerer
{
    Passage _overview = null!;

    void Establish()
    {
        _overview = Passage(0.3, 1.0 / 61) with
        {
            Page = new("https://cratis.io/why-cratis/"),
            Content = "Why developers choose Cratis.",
            IsSummary = true,
            IsOverviewBoost = true
        };
        Found(
            _overview,
            Passage(0.3, 2.0 / 61) with { Page = new("https://cratis.io/unrelated/"), Content = "ORDINARY_WEAK_PASSAGE" });
    }

    async Task Because() => result = await answers.For(new("What is Cratis good at?"), "cli");

    [Fact] void should_not_refuse() => result.IsRefusal.ShouldBeFalse();
    [Fact] void should_admit_the_overview_boost_despite_the_weak_score() => sent[1].Text.ShouldContain("Why developers choose Cratis.");
    [Fact] void should_still_exclude_an_ordinary_weak_passage() => sent[1].Text.ShouldNotContain("ORDINARY_WEAK_PASSAGE");
    [Fact] void should_cite_the_overview_page() => result.Citations.SequenceEqual([_overview.Page]).ShouldBeTrue();
    [Fact] void should_report_the_overview_boost_similarity() => result.Confidence.ShouldEqual(0.3);
}
