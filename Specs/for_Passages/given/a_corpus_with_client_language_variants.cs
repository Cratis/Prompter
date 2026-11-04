// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_Passages.given;

/// <summary>
/// A corpus where 25 Elixir client chunks lean closer to the query axis than the one .NET page, so they would
/// fill the whole semantic candidate pool unless variant pages are kept out of it.
/// </summary>
public class a_corpus_with_client_language_variants : a_disposable_documentation_corpus
{
    protected const string DefaultPage = "https://cratis.io/chronicle/events/concurrency.md";
    protected const string VariantPagePrefix = "https://cratis.io/chronicle/clients/elixir/";

    /// <summary>Gets the similarity of the .NET page to the query axis.</summary>
    protected virtual float DefaultSimilarity => 0.7f;

    async Task Establish()
    {
        await Seed(
            new(new("default"), new(DefaultPage), "Concurrency", "Overview", "Optimistic concurrency for the .NET client.", new("synthetic")),
            Axes((4, DefaultSimilarity), (5, MathF.Sqrt(1 - (DefaultSimilarity * DefaultSimilarity)))));

        for (var index = 0; index < 25; index++)
        {
            await Seed(
                new(new($"variant_{index}"), new($"{VariantPagePrefix}concurrency-{index}/"), "Concurrency", "Overview", "Optimistic concurrency for the Elixir client.", new("synthetic")),
                Axes((4, 0.95f), (100 + index, MathF.Sqrt(1 - (0.95f * 0.95f)))));
        }
    }
}
