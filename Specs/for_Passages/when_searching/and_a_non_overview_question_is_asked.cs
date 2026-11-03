// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_Passages.when_searching;

public class and_a_non_overview_question_is_asked : given.a_corpus_with_overview_pages
{
    async Task Because()
    {
        if (HasDatabase)
        {
            await answers.For(new(PositiveQuestion), "cli");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_not_prepend_the_overview_summary()
    {
        RequireDatabase();
        retrieved.Any(passage => passage.ChunkId == OverviewSummaryId).ShouldBeFalse();
    }
}
