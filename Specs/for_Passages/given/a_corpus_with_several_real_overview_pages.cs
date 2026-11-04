// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_Passages.given;

/// <summary>
/// A corpus with all three curated overview pages (index, cratis-stack, why-cratis) admitted at once, each
/// clearing the similarity floor by a different margin, as happens in production. Only why-cratis.md's body
/// names Arc, and it is also the weakest of the three.
/// </summary>
public class a_corpus_with_several_real_overview_pages : a_disposable_documentation_corpus
{
    protected const string IndexBodyId = "https://cratis.io/index.md#1";
    protected const string StackBodyId = "https://cratis.io/cratis-stack.md#1";
    protected const string WhyCratisBodyId = "https://cratis.io/why-cratis.md#1";

    async Task Establish()
    {
        await Seed(
            new(new("https://cratis.io/index.md#summary"), new("https://cratis.io/index.md"), "Index", "Index > Summary", "Cratis\n\nThe Cratis stack.", new("synthetic")),
            Axes((4, 0.95f), (6, MathF.Sqrt(1 - (0.95f * 0.95f)))));
        await Seed(
            new(new(IndexBodyId), new("https://cratis.io/index.md"), "Index", "Index > Payoff", "Chronicle records the facts that drive Cratis applications.", new("synthetic")),
            Axes((4, 0.95f), (7, MathF.Sqrt(1 - (0.95f * 0.95f)))));

        await Seed(
            new(new("https://cratis.io/cratis-stack.md#summary"), new("https://cratis.io/cratis-stack.md"), "Cratis Stack", "Cratis Stack > Summary", "Cratis Stack\n\nThe products behind Cratis.", new("synthetic")),
            Axes((4, 0.9f), (8, MathF.Sqrt(1 - (0.9f * 0.9f)))));
        await Seed(
            new(new(StackBodyId), new("https://cratis.io/cratis-stack.md"), "Cratis Stack", "Cratis Stack > Products", "The Cratis Stack spans Chronicle, Components, and the CLI tooling.", new("synthetic")),
            Axes((4, 0.9f), (9, MathF.Sqrt(1 - (0.9f * 0.9f)))));

        await Seed(
            new(new("https://cratis.io/why-cratis.md#summary"), new("https://cratis.io/why-cratis.md"), "Why Cratis", "Why Cratis > Summary", "Why developers choose Cratis\n\nThe practical reasons teams choose Cratis.", new("synthetic")),
            Axes((4, 0.6f), (10, MathF.Sqrt(1 - (0.6f * 0.6f)))));
        await Seed(
            new(new(WhyCratisBodyId), new("https://cratis.io/why-cratis.md"), "Why Cratis", "Why Cratis > Developer payoff", "Arc shapes the application layer that sits on top of Chronicle.", new("synthetic")),
            Axes((4, 0.6f), (11, MathF.Sqrt(1 - (0.6f * 0.6f)))));

        for (var index = 0; index < 21; index++)
        {
            await Seed(
                new(new($"distractor_{index}"), new($"https://cratis.io/distractor/{index}/"), "Distractor", "Entries", $"Quasar widgets entry {index}.", new("synthetic")),
                Axes((4, 0.8f), (12, 0.6f)));
        }
    }
}
