// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_Passages.when_searching;

public class and_more_rows_match_than_the_limit : given.a_corpus_with_a_semantic_crowd
{
    const string Best = "https://cratis.io/zbest/";
    const string Early = "https://cratis.io/aearly/";

    async Task Because()
    {
        if (HasDatabase)
        {
            // A second admitted anchor on a small page that sorts first alphabetically (weaker match, no full-text hit) and has enough chunks to fill the row limit.
            for (var index = 0; index < 70; index++)
            {
                await Seed(ChunkOf(Early, index, "Early", $"Early chunk {index}."), index == 1 ? Axes((QueryAxis, 0.9f), (99, 0.4359f)) : Orthogonal(100 + index));
            }

            await Seed(ChunkOf(Best, 0, "Best", "Introduction."), Orthogonal(20));
            await Seed(ChunkOf(Best, 1, "Best > Section", "Best anchor reedwarbler."), Query());
            await Seed(ChunkOf(Best, 2, "Best > Section > Deep", "Best detail."), Orthogonal(21));
            AskWith(Query());
            await answers.For(new("reedwarbler"), "cli", TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_keep_the_best_anchor_section_before_cutting()
    {
        RequireDatabase();
        retrieved.Where(passage => passage.IsSectionExpansion).Select(passage => passage.ChunkId).ShouldContain($"{Best}#2");
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_not_exceed_the_row_limit()
    {
        RequireDatabase();
        retrieved.Count(passage => passage.IsSectionExpansion).ShouldBeLessThanOrEqual(64);
    }
}
