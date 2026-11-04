// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_Answers.when_retrieving;

public class and_several_curated_overview_pages_are_admitted : given.a_grounded_answerer
{
    void Establish()
    {
        options.Answering.MinSemanticSimilarity = 0.5;
        Found(
            Passage(0.95) with { Page = new("https://cratis.io/index.md"), HeadingPath = "Index > Summary", Content = "Cratis overview", IsSummary = true, IsCurated = true },
            Passage(0.95) with { Page = new("https://cratis.io/index.md"), HeadingPath = "Index > Payoff", Content = "Chronicle records the facts that drive Cratis applications.", IsPageExpansion = true, IsCurated = true },
            Passage(0.9) with { Page = new("https://cratis.io/cratis-stack.md"), HeadingPath = "Stack > Summary", Content = "The Cratis stack", IsSummary = true, IsCurated = true },
            Passage(0.9) with { Page = new("https://cratis.io/cratis-stack.md"), HeadingPath = "Stack > Products", Content = "The Cratis Stack spans Chronicle, Components, and the CLI tooling.", IsPageExpansion = true, IsCurated = true },
            Passage(0.6) with { Page = new("https://cratis.io/why-cratis.md"), HeadingPath = "Why Cratis > Summary", Content = "Why developers choose Cratis", IsSummary = true, IsCurated = true },
            Passage(0.6) with { Page = new("https://cratis.io/why-cratis.md"), HeadingPath = "Why Cratis > Developer payoff", Content = "Arc shapes the application layer that sits on top of Chronicle.", IsPageExpansion = true, IsCurated = true });
    }

    async Task Because() => result = await answers.For(new("What is Cratis good at?"), "spec", TestContext.Current.CancellationToken);

    [Fact] void should_ground_on_every_admitted_overview_page() => result.Passages.Select(passage => passage.Page.Value).Distinct().Count().ShouldEqual(3);
    [Fact] void should_include_the_weakest_page_s_body() => result.Passages.Select(passage => passage.Content).ShouldContain("Arc shapes the application layer that sits on top of Chronicle.");
}
