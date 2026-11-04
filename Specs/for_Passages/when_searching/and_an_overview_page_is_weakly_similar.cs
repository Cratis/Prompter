// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_Passages.when_searching;

public class and_an_overview_page_is_weakly_similar : given.a_corpus_with_a_weakly_similar_overview_page
{
    async Task Because()
    {
        if (HasDatabase)
        {
            await answers.For(new(OverviewQuestion), "cli");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_read_the_page_body_as_a_curated_expansion()
    {
        RequireDatabase();
        var body = retrieved.Single(passage => passage.ChunkId == OverviewBodyId && passage.IsPageExpansion);
        body.IsCurated.ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_ground_the_answer_on_the_page_body()
    {
        RequireDatabase();
        sent[^1].Text.ShouldContain("Arc shapes the application");
    }
}
