// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_Passages.when_searching;

public class and_an_admitted_chunk_has_a_section : given.a_corpus_with_a_semantic_crowd
{
    const string Page = "https://cratis.io/broad/";

    async Task Because()
    {
        if (HasDatabase)
        {
            await Seed(ChunkOf(Page, 0, "Broad", "Introduction."), Orthogonal(20));
            await Seed(ChunkOf(Page, 1, "Broad > Strengths", "Strengths overview zebrafinch."), Query());
            await Seed(ChunkOf(Page, 2, "Broad > Strengths > Events", "Events detail."), Orthogonal(21));
            await Seed(ChunkOf(Page, 3, "Broad > Strengths > Arc", "Arc detail."), Orthogonal(22));
            await Seed(ChunkOf(Page, 4, "Broad > Other", new string('o', 5000)), Orthogonal(23));
            AskWith(Query());
            await answers.For(new("zebrafinch"), "cli", TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_return_the_neighbor_and_the_section_flagged_as_expansions()
    {
        RequireDatabase();
        var expansions = retrieved.Where(passage => passage.IsSectionExpansion).Select(passage => passage.ChunkId).ToArray();
        expansions.ShouldContain($"{Page}#0");
        expansions.ShouldContain($"{Page}#2");
        expansions.ShouldContain($"{Page}#3");
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_not_return_an_unrelated_section_of_a_large_page()
    {
        RequireDatabase();
        retrieved.Select(passage => passage.ChunkId).ShouldNotContain($"{Page}#4");
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_not_flag_the_anchor_itself()
    {
        RequireDatabase();
        retrieved.Single(passage => passage.ChunkId == $"{Page}#1").IsSectionExpansion.ShouldBeFalse();
    }
}
