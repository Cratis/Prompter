// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_Answers.when_applying_the_retrieval_gate;

public class and_more_passages_qualify_than_the_limit : given.a_grounded_answerer
{
    Passage[] _all = [];

    void Establish()
    {
        _all = [.. Enumerable.Range(0, 12).Select(index => Passage(0.6 + (index * 0.025), 1.0 / (61 + index)) with { Page = new($"https://cratis.io/page-{index}/") })];
        Found(_all);
    }

    async Task Because() => result = await answers.For(new("What is Cratis Chronicle?"), "cli");

    [Fact] void should_answer() => result.IsRefusal.ShouldBeFalse();
    [Fact] void should_admit_the_first_passages_in_rrf_order() => result.Passages.SequenceEqual(_all.Take(8)).ShouldBeTrue();
    [Fact] void should_keep_at_most_four_citations() => result.Citations.Count().ShouldEqual(4);
    [Fact] void should_report_the_first_passage_similarity() => result.Confidence.ShouldEqual(_all[0].SemanticSimilarity);
}
