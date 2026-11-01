// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_applying_the_retrieval_gate;

public class and_the_best_passage_ranks_below_the_admission_limit : given.a_grounded_answerer
{
    Passage _best = null!;

    void Establish()
    {
        _best = Passage(0.6, 1.0 / 70) with { Page = new("https://cratis.io/cratis-stack/"), Content = "BEST_OVERVIEW" };
        var crowd = Enumerable.Range(0, 8)
            .Select(index => Passage(0.3, 2.0 / 61) with { Page = new($"https://cratis.io/crowd/{index}/"), Content = $"CROWD_{index}" });
        Found([.. crowd, _best]);
    }

    async Task Because() => result = await answers.For(new("What are all Cratis products?"), "cli");

    [Fact] void should_answer() => result.IsRefusal.ShouldBeFalse();
    [Fact] void should_admit_only_the_qualified_passage() => result.Passages.SequenceEqual([_best]).ShouldBeTrue();
    [Fact] void should_send_the_qualified_passage() => sent[1].Text.ShouldContain(_best.Content);
    [Fact] void should_not_send_the_weak_passages() => sent[1].Text.ShouldNotContain("CROWD_");
    [Fact] void should_report_the_admitted_similarity() => result.Confidence.ShouldEqual(0.6);
    [Fact] void should_search_the_whole_candidate_pool() => passages.Received(1).Search(Arg.Any<Question>(), Passages.MaximumCandidates, Arg.Any<CancellationToken>());
}
