// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_Passages.when_searching;

public class and_several_overview_pages_are_admitted : given.a_corpus_with_several_real_overview_pages
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
    void should_read_each_admitted_page_s_body_as_a_curated_expansion()
    {
        RequireDatabase();
        retrieved.Count(passage => passage.ChunkId == IndexBodyId && passage.IsPageExpansion).ShouldEqual(1);
        retrieved.Count(passage => passage.ChunkId == StackBodyId && passage.IsPageExpansion).ShouldEqual(1);
        retrieved.Count(passage => passage.ChunkId == WhyCratisBodyId && passage.IsPageExpansion).ShouldEqual(1);
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_ground_the_answer_on_the_weakest_page_s_body()
    {
        RequireDatabase();
        sent[^1].Text.ShouldContain("Arc shapes the application layer that sits on top of Chronicle.");
    }
}
