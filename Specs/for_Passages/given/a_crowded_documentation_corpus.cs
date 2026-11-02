// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Ingestion;

namespace Cratis.Prompter.Specs.for_Passages.given;

/// <summary>
/// A corpus where eight chunks match the question lexically and semantically (weakly), crowding the one
/// strongly similar overview chunk out of the reciprocal rank fusion top eight.
/// </summary>
public class a_crowded_documentation_corpus : a_disposable_documentation_corpus
{
    protected const string OverviewContent = "Every product in one table.";
    protected const string CrowdContent = "zephyr quasar widgets, entry";

    async Task Establish()
    {
        await Seed(
            new(new("overview"), new("https://cratis.io/cratis-stack/"), "The Cratis Stack", "Capability map", OverviewContent, new("synthetic")),
            Axes((4, 0.7f), (5, MathF.Sqrt(0.51f))));

        for (var index = 0; index < 8; index++)
        {
            await Seed(
                new(new($"crowd_{index}"), new($"https://cratis.io/crowd/{index}/"), "Crowd", "Entries", $"Cratis ships {CrowdContent} {index}.", new("synthetic")),
                Axes((4, 0.3f), (6, MathF.Sqrt(0.91f))));
        }
    }
}
