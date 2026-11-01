// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;

namespace Cratis.Prompter.Specs.for_Passages.when_searching;

public class and_lexical_matches_crowd_out_the_best_semantic_match : given.a_crowded_documentation_corpus
{
    Answer _result = null!;

    async Task Because()
    {
        if (HasDatabase)
        {
            _result = await answers.For(new(CrowdedQuestion), "cli");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    void should_answer_from_the_strong_passage_beyond_the_rrf_top_eight()
    {
        RequireDatabase();
        _result.IsRefusal.ShouldBeFalse();
        _result.Passages.Single().Page.Value.ShouldEqual("https://cratis.io/cratis-stack/");
        sent[1].Text.ShouldContain(OverviewContent);
        sent[1].Text.ShouldNotContain(CrowdContent);
        (retrieved.Count > 8).ShouldBeTrue();
        retrieved.ToList().FindIndex(passage => passage.Content == OverviewContent).ShouldEqual(8);
    }
}
