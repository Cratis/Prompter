// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_Passages.when_searching;

public class and_no_chunk_contains_every_term : given.a_corpus_with_a_semantic_crowd
{
    async Task Because()
    {
        if (HasDatabase)
        {
            await SeedCrowd();
            await Seed(ChunkOf("https://cratis.io/first/", 0, "Broad", "Xylophone ensembles are described here."), Orthogonal(20));
            await Seed(ChunkOf("https://cratis.io/second/", 0, "Broad", "Quartz resonators are described here."), Orthogonal(21));
            AskWith(Query());
            await answers.For(new("xylophone quartz"), "cli", TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_retrieve_chunks_matching_only_some_terms()
    {
        RequireDatabase();
        retrieved.Select(passage => passage.Page.Value).ShouldContain("https://cratis.io/first/");
        retrieved.Select(passage => passage.Page.Value).ShouldContain("https://cratis.io/second/");
    }
}
