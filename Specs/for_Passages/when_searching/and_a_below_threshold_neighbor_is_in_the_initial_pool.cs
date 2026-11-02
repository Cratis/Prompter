// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_Passages.when_searching;

public class and_a_below_threshold_neighbor_is_in_the_initial_pool : given.a_corpus_with_a_semantic_crowd
{
    const string Page = "https://cratis.io/pool/";

    async Task Because()
    {
        if (HasDatabase)
        {
            await Seed(ChunkOf(Page, 0, "Broad", "Introduction."), Orthogonal(20));
            await Seed(ChunkOf(Page, 1, "Broad > First", "Anchor text pondweed."), Query());
            await Seed(ChunkOf(Page, 2, "Broad > Second", "Neighbor detail kelpforest."), Axes((QueryAxis, 0.4f), (60, 0.9165f)));
            AskWith(Query());
            await answers.For(new("pondweed"), "cli", TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_have_the_neighbor_in_the_pool_below_the_threshold()
    {
        RequireDatabase();
        retrieved.Single(passage => passage.ChunkId == $"{Page}#2" && !passage.IsSectionExpansion).SemanticSimilarity.ShouldBeLessThan(0.5);
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_still_send_the_neighbor_to_the_model()
    {
        RequireDatabase();
        sent[1].Text.ShouldContain("Neighbor detail kelpforest.");
    }
}
