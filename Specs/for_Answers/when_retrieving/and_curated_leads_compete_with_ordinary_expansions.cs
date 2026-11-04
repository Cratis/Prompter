// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_Answers.when_retrieving;

public class and_curated_leads_compete_with_ordinary_expansions : given.a_grounded_answerer
{
    static Passage OnPage(string page, double similarity) => Passage(similarity) with { Page = new($"https://cratis.io/{page}") };

    void Establish()
    {
        options.Answering.MaxPassages = 4;
        Found(
            OnPage("why-cratis.md", 0.5) with { IsSummary = true, IsCurated = true, HeadingPath = "why > Summary", Content = "Why summary" },
            OnPage("chronicle/", 0.7) with { IsSummary = true, HeadingPath = "chronicle > Summary", Content = "Chronicle summary" },
            OnPage("chronicle/", 0.9) with { IsPageExpansion = true, Content = "Ordinary best" },
            OnPage("chronicle/", 0.85) with { IsPageExpansion = true, Content = "Ordinary second" },
            OnPage("why-cratis.md", 0.2) with { IsPageExpansion = true, IsCurated = true, HeadingPath = "why > Lead", Content = "Curated lead" });
    }

    async Task Because() => result = await answers.For(new("What is Cratis good at?"), "spec", TestContext.Current.CancellationToken);

    [Fact] void should_keep_the_curated_lead_ahead_of_ordinary_expansions() => result.Passages.Select(passage => passage.Content).ShouldContain("Curated lead");
    [Fact] void should_keep_the_ordinary_cap() => result.Passages.Count(passage => passage.IsPageExpansion).ShouldEqual(2);
    [Fact] void should_drop_the_lower_ordinary_expansion() => result.Passages.Select(passage => passage.Content).ShouldNotContain("Ordinary second");
    [Fact] void should_stay_within_the_passage_budget() => result.Passages.Count.ShouldEqual(4);
}
