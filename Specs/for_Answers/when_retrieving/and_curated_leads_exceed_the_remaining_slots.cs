// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_Answers.when_retrieving;

public class and_curated_leads_exceed_the_remaining_slots : given.a_grounded_answerer
{
    static Passage OnPage(string page, double similarity) => Passage(similarity) with { Page = new($"https://cratis.io/{page}") };

    void Establish()
    {
        options.Answering.MaxPassages = 4;
        Found(
            OnPage("index.md", 0.5) with { IsSummary = true, IsCurated = true, HeadingPath = "index > Summary", Content = "Index summary" },
            OnPage("why-cratis.md", 0.5) with { IsSummary = true, IsCurated = true, HeadingPath = "why > Summary", Content = "Why summary" },
            OnPage("cratis-stack.md", 0.5) with { IsSummary = true, IsCurated = true, HeadingPath = "stack > Summary", Content = "Stack summary" },
            OnPage("index.md", 0.2) with { IsPageExpansion = true, IsCurated = true, HeadingPath = "index > Facts", Content = "Index lead" },
            OnPage("why-cratis.md", 0.2) with { IsPageExpansion = true, IsCurated = true, HeadingPath = "why > Lead", Content = "Why lead" },
            OnPage("cratis-stack.md", 0.2) with { IsPageExpansion = true, IsCurated = true, HeadingPath = "stack > Lead", Content = "Stack lead" });
    }

    async Task Because() => result = await answers.For(new("What is Cratis good at?"), "spec", TestContext.Current.CancellationToken);

    [Fact] void should_admit_only_the_remaining_slot() => result.Passages.Count(passage => passage.IsPageExpansion).ShouldEqual(1);
    [Fact] void should_stay_within_the_passage_budget() => result.Passages.Count.ShouldEqual(4);
}
