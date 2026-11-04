// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_Passages.when_searching;

public class and_overview_pages_have_leads : given.a_corpus_with_curated_overview_leads
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
    void should_read_each_curated_page_lead()
    {
        RequireDatabase();
        retrieved.Where(passage => passage.IsPageExpansion && passage.IsCurated).Select(passage => passage.ChunkId)
            .Order().ShouldEqual([IndexLeadId, WhyLeadId]);
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_order_chunks_by_number()
    {
        RequireDatabase();
        retrieved.Where(passage => passage.IsPageExpansion && passage.IsCurated).Select(passage => passage.ChunkId).ShouldNotContain(WhyLaterId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_ground_the_answer_on_every_lead()
    {
        RequireDatabase();
        sent[^1].Text.ShouldContain(WhyLeadText);
        sent[^1].Text.ShouldContain(IndexLeadText);
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_not_read_later_bodies_by_similarity()
    {
        RequireDatabase();
        var curated = retrieved.Where(passage => passage.IsPageExpansion && passage.IsCurated).Select(passage => passage.ChunkId).ToArray();
        curated.ShouldNotContain(WhyLaterId);
        curated.ShouldNotContain(WhyLatestId);
        curated.ShouldNotContain(IndexLaterId);
    }
}
