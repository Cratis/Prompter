// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_Passages.when_searching;

public class and_no_chunk_is_admitted : given.a_corpus_with_a_semantic_crowd
{
    const string Page = "https://cratis.io/tiny/";

    async Task Because()
    {
        if (HasDatabase)
        {
            await Seed(ChunkOf(Page, 0, "Broad", "Introduction."), Orthogonal(20));
            await Seed(ChunkOf(Page, 1, "Broad > First", "Anchor text narwhalpod."), Orthogonal(21));
            AskWith(Orthogonal(50));
            await answers.For(new("narwhalpod"), "cli", TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_return_no_section_expansions()
    {
        RequireDatabase();
        retrieved.Any(passage => passage.IsSectionExpansion).ShouldBeFalse();
    }
}
