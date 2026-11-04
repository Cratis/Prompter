// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_Passages.when_searching;

public class and_an_overview_question_is_asked : given.a_corpus_with_overview_pages
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
    void should_put_the_overview_summary_first()
    {
        RequireDatabase();
        retrieved[0].ChunkId.ShouldEqual(OverviewSummaryId);
        retrieved[0].IsSummary.ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_mark_it_as_curated()
    {
        RequireDatabase();
        retrieved[0].IsCurated.ShouldBeTrue();
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_not_duplicate_it()
    {
        RequireDatabase();
        retrieved.Count(passage => passage.ChunkId == OverviewSummaryId).ShouldEqual(1);
    }
}
