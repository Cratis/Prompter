// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_Answers.when_retrieving;

public class and_a_curated_overview_summary_is_below_the_floor : given.a_grounded_answerer
{
    void Establish()
    {
        options.Answering.MinSemanticSimilarity = 0.5;
        Found(
            Passage(0.42) with { IsSummary = true, IsCurated = true, Content = "Curated overview" },
            Passage(0.42) with { Page = new("https://cratis.io/other/"), IsSummary = true, Content = "Uncurated weak summary" },
            Passage(double.NaN) with { Page = new("https://cratis.io/broken/"), IsSummary = true, IsCurated = true, Content = "Curated but invalid" },
            Passage(0.8) with { Page = new("https://untrusted.example/page"), IsSummary = true, IsCurated = true, Content = "Curated but not citable" });
    }

    async Task Because() => result = await answers.For(new("What is Cratis good at?"), "spec", TestContext.Current.CancellationToken);

    [Fact] void should_ground_on_the_curated_summary_only() => result.Passages.Select(passage => passage.Content).ShouldEqual(["Curated overview"]);
}
