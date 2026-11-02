// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_Passages.when_searching;

public class and_a_precise_question_matches_every_term : given.a_corpus_with_a_semantic_crowd
{
    async Task Because()
    {
        if (HasDatabase)
        {
            await SeedCrowd();
            for (var index = 0; index < 3; index++)
            {
                await Seed(ChunkOf($"https://cratis.io/partial{index}/", 0, "Broad", "Alpha alpha alpha alpha alpha alpha alpha alpha alpha alpha."), Orthogonal(20 + index));
            }

            await Seed(ChunkOf("https://cratis.io/complete/", 0, "Broad", "Alpha, beta and gamma together."), Orthogonal(30));
            AskWith(Query());
            await answers.For(new("alpha beta gamma"), "cli", TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_rank_the_all_term_chunk_above_every_partial_match()
    {
        RequireDatabase();
        var complete = retrieved.Single(passage => passage.Page.Value == "https://cratis.io/complete/");
        var partials = retrieved.Where(passage => passage.Page.Value.Contains("/partial", StringComparison.Ordinal)).ToArray();
        partials.Length.ShouldEqual(3);
        partials.All(partial => complete.Score > partial.Score).ShouldBeTrue();
    }
}
