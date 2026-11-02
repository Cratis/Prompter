// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_Passages.when_searching;

public class and_an_admitted_chunk_is_on_a_small_page : given.a_corpus_with_a_semantic_crowd
{
    const string Page = "https://cratis.io/tiny/";

    async Task Because()
    {
        if (HasDatabase)
        {
            await Seed(ChunkOf(Page, 0, "Broad", "Introduction."), Orthogonal(20));
            await Seed(ChunkOf(Page, 1, "Broad > First", "Anchor text narwhalpod."), Query());
            await Seed(ChunkOf(Page, 2, "Broad > Second", "Second."), Orthogonal(21));
            await Seed(ChunkOf(Page, 3, "Broad > Third", "Third."), Orthogonal(22));
            await Seed(ChunkOf(Page, 4, "Broad > Fourth", "Fourth."), Orthogonal(23));
            AskWith(Query());
            await answers.For(new("narwhalpod"), "cli", TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_return_every_chunk_of_the_page()
    {
        RequireDatabase();
        retrieved.Where(passage => passage.Page.Value == Page).Select(passage => passage.ChunkId).Order().ToArray()
            .ShouldEqual([$"{Page}#0", $"{Page}#1", $"{Page}#2", $"{Page}#3", $"{Page}#4"]);
    }
}
