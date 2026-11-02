// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_Passages.when_searching;

public class and_only_the_brand_name_matches : given.a_corpus_with_a_semantic_crowd
{
    async Task Because()
    {
        if (HasDatabase)
        {
            await SeedCrowd();
            await Seed(ChunkOf("https://cratis.no/llms.txt", 0, "Cratis", "Cratis helps teams. Cratis Build, Cratis Studio and Cratis Assurance."), Orthogonal(20));
            AskWith(Query());
            await answers.For(new("what is Cratis, in a nutshell?"), "cli", TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_not_rank_a_chunk_by_brand_density()
    {
        RequireDatabase();
        retrieved.Select(passage => passage.Page.Value).ShouldNotContain("https://cratis.no/llms.txt");
    }
}
