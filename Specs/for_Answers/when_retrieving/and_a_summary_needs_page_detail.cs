// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_Answers.when_retrieving;

public class and_a_summary_needs_page_detail : given.a_grounded_answerer
{
    void Establish()
    {
        options.Answering.MaxPassages = 3;
        Found(
            Passage(0.9) with { Page = new("https://cratis.io/cratis-stack/"), HeadingPath = "Stack > Summary", Content = "Lists all products", IsSummary = true },
            Passage(0.8) with { Content = "Ordinary candidate" },
            Passage(0.7) with { Content = "Tail candidate" },
            Passage(0.6) with { Page = new("https://cratis.io/cratis-stack/"), HeadingPath = "Stack > Capability map", Content = "Chronicle: event sourcing. Arc: CQRS. Components: React UI.", IsPageExpansion = true },
            Passage(0.5) with { Page = new("https://cratis.io/cratis-stack/"), Content = "Product details", IsPageExpansion = true },
            Passage(0.4) with { Page = new("https://cratis.io/cratis-stack/"), Content = "Third detail", IsPageExpansion = true });
    }

    async Task Because() => result = await answers.For(new("What are all cratis products and what do they do?"), "spec", TestContext.Current.CancellationToken);

    [Fact] void should_keep_the_original_summary() => result.Passages.Any(passage => passage.IsSummary).ShouldBeTrue();
    [Fact] void should_include_actual_product_descriptions() => sent[1].Text.ShouldContain("Chronicle: event sourcing. Arc: CQRS. Components: React UI.");
    [Fact] void should_stay_within_the_passage_budget() => result.Passages.Count.ShouldEqual(3);
    [Fact] void should_reserve_no_more_than_two_expansion_slots() => result.Passages.Count(passage => passage.IsPageExpansion).ShouldEqual(2);
    [Fact] void should_cite_one_page_for_summary_and_detail() => result.Citations.Select(page => page.Value).ShouldEqual(["https://cratis.io/cratis-stack/"]);
}
