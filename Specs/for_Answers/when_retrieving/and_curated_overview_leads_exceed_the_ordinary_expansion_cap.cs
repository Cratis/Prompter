// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_Answers.when_retrieving;

public class and_curated_overview_leads_exceed_the_ordinary_expansion_cap : given.a_grounded_answerer
{
    static Passage OnPage(string page, double similarity) => Passage(similarity) with { Page = new($"https://cratis.io/{page}") };

    void Establish() => Found(
        OnPage("index.md", 0.5) with { IsSummary = true, IsCurated = true, HeadingPath = "index > Summary", Content = "Index summary" },
        OnPage("why-cratis.md", 0.5) with { IsSummary = true, IsCurated = true, HeadingPath = "why > Summary", Content = "Why summary" },
        OnPage("cratis-stack.md", 0.5) with { IsSummary = true, IsCurated = true, HeadingPath = "stack > Summary", Content = "Stack summary" },
        Passage(0.8) with { Content = "Ordinary one" },
        Passage(0.75) with { Content = "Ordinary two" },
        Passage(0.7) with { Content = "Ordinary three" },
        Passage(0.6) with { Content = "Ordinary four" },
        OnPage("index.md", 0.2) with { IsPageExpansion = true, IsCurated = true, HeadingPath = "index > Facts", Content = "Arc is the CQRS application framework" },
        OnPage("why-cratis.md", 0.2) with { IsPageExpansion = true, IsCurated = true, HeadingPath = "why > Lead", Content = "Chronicle records the facts, Arc shapes the application" },
        OnPage("cratis-stack.md", 0.2) with { IsPageExpansion = true, IsCurated = true, HeadingPath = "stack > Lead", Content = "From a sticky note to a system" });

    async Task Because() => result = await answers.For(new("What is Cratis good at?"), "spec", TestContext.Current.CancellationToken);

    [Fact] void should_admit_every_curated_lead() => result.Passages.Count(passage => passage.IsPageExpansion).ShouldEqual(3);
    [Fact] void should_stay_within_the_passage_budget() => result.Passages.Count.ShouldEqual(options.Answering.MaxPassages);
    [Fact] void should_keep_every_summary() => result.Passages.Count(passage => passage.IsSummary).ShouldEqual(3);
    [Fact] void should_send_the_product_naming_lead() => sent[^1].Text.ShouldContain("Arc shapes the application");
}
