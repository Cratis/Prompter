// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_Passages.given;

/// <summary>
/// A corpus where the why-cratis summary and its body both fall below the similarity threshold for the overview
/// question, as a differently worded overview page does in production.
/// </summary>
public class a_corpus_with_a_weakly_similar_overview_page : a_disposable_documentation_corpus
{
    protected const string OverviewSummaryId = "https://cratis.io/why-cratis.md#summary";
    protected const string OverviewBodyId = "https://cratis.io/why-cratis.md#1";

    async Task Establish()
    {
        await Seed(
            new(new(OverviewSummaryId), new("https://cratis.io/why-cratis.md"), "Why Cratis", "why-cratis > Summary", "Why developers choose Cratis\n\nThe practical reasons teams choose Cratis.", new("synthetic")),
            Axes((4, 0.3f), (5, 0.954f)));
        await Seed(
            new(new(OverviewBodyId), new("https://cratis.io/why-cratis.md"), "Why Cratis", "why-cratis > The pieces fit together", "Chronicle records the facts and Arc shapes the application.", new("synthetic")),
            Axes((4, 0.3f), (6, 0.954f)));
    }
}
