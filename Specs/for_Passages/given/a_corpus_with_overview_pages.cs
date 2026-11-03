// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_Passages.given;

/// <summary>
/// A corpus where the why-cratis summary clears the similarity threshold but twenty-one closer chunks
/// keep it out of both retrieval arms' candidate pools.
/// </summary>
public class a_corpus_with_overview_pages : a_disposable_documentation_corpus
{
    protected const string OverviewSummaryId = "https://cratis.io/why-cratis.md#summary";

    async Task Establish()
    {
        await Seed(
            new(new(OverviewSummaryId), new("https://cratis.io/why-cratis.md"), "Why Cratis", "why-cratis > Summary", "Why developers choose Cratis\n\nThe practical reasons teams choose Cratis.", new("synthetic")),
            Axes((4, 0.6f), (5, 0.8f)));

        for (var index = 0; index < 21; index++)
        {
            await Seed(
                new(new($"distractor_{index}"), new($"https://cratis.io/distractor/{index}/"), "Distractor", "Entries", $"Quasar widgets entry {index}.", new("synthetic")),
                Axes((4, 0.8f), (6, 0.6f)));
        }
    }
}
