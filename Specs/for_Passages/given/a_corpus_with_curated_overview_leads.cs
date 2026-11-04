// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_Passages.given;

/// <summary>
/// A corpus with two curated overview pages whose lead is the lowest-ordered body chunk by number but far from the
/// question, behind later bodies that are closer and sort before the lead as text (<c>#10</c> before <c>#2</c>).
/// </summary>
public class a_corpus_with_curated_overview_leads : a_disposable_documentation_corpus
{
    protected const string WhySummaryId = "https://cratis.io/why-cratis.md#summary";
    protected const string WhyLeadId = "https://cratis.io/why-cratis.md#2";
    protected const string WhyLaterId = "https://cratis.io/why-cratis.md#10";
    protected const string WhyLatestId = "https://cratis.io/why-cratis.md#11";
    protected const string IndexSummaryId = "https://cratis.io/index.md#summary";
    protected const string IndexLeadId = "https://cratis.io/index.md#0";
    protected const string IndexLaterId = "https://cratis.io/index.md#1";
    protected const string WhyLeadText = "Chronicle records the facts, Arc shapes the application, Components renders it.";
    protected const string IndexLeadText = "Arc is the CQRS application framework with generated TypeScript proxies.";

    async Task Establish()
    {
        await Seed(
            new(new(WhySummaryId), new("https://cratis.io/why-cratis.md"), "Why Cratis", "why-cratis > Summary", "Why developers choose Cratis\n\nThe practical reasons teams choose Cratis.", new("synthetic")),
            Axes((4, 0.6f), (5, 0.8f)));
        await Seed(
            new(new(WhyLeadId), new("https://cratis.io/why-cratis.md"), "Why Cratis", "why-cratis > The pieces fit together", WhyLeadText, new("synthetic")),
            Axes((4, 0.1f), (6, 0.995f)));
        await Seed(
            new(new(WhyLaterId), new("https://cratis.io/why-cratis.md"), "Why Cratis", "why-cratis > Teams", "Teams onboard faster with shared conventions.", new("synthetic")),
            Axes((4, 0.7f), (7, 0.714f)));
        await Seed(
            new(new(WhyLatestId), new("https://cratis.io/why-cratis.md"), "Why Cratis", "why-cratis > Reviews", "Reviews stay sharper.", new("synthetic")),
            Axes((4, 0.65f), (11, 0.76f)));
        await Seed(
            new(new(IndexSummaryId), new("https://cratis.io/index.md"), "Cratis documentation", "index > Summary", "Cratis documentation\n\nCratis is an open-source event sourcing and CQRS platform.", new("synthetic")),
            Axes((4, 0.55f), (8, 0.835f)));
        await Seed(
            new(new(IndexLeadId), new("https://cratis.io/index.md"), "Cratis documentation", "index > Facts", IndexLeadText, new("synthetic")),
            Axes((4, 0.2f), (9, 0.98f)));
        await Seed(
            new(new(IndexLaterId), new("https://cratis.io/index.md"), "Cratis documentation", "index > Paths", "Start with a learning path.", new("synthetic")),
            Axes((4, 0.7f), (10, 0.714f)));

        // Closer chunks fill both retrieval arms' candidate pools, so the leads arrive only as curated expansions.
        for (var index = 0; index < 21; index++)
        {
            await Seed(
                new(new($"distractor_{index}"), new($"https://cratis.io/distractor/{index}/"), "Distractor", "Entries", $"Quasar widgets entry {index}.", new("synthetic")),
                Axes((4, 0.8f), (12, 0.6f)));
        }
    }
}
