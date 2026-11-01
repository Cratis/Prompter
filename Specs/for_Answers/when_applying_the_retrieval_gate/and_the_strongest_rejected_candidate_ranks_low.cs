// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Retrieval;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_applying_the_retrieval_gate;

public class and_the_strongest_rejected_candidate_ranks_low : given.a_grounded_answerer
{
    Passage[] _all = [];

    void Establish()
    {
        _all = [.. Enumerable.Range(0, 10).Select(index => Passage(index == 9 ? 0.45 : 0.2, 1.0 / (61 + index)) with { Page = new($"https://cratis.io/page-{index}/") })];
        Found(_all);
    }

    async Task Because() => result = await answers.For(new("What is the weather today?"), "cli");

    [Fact] void should_refuse() => result.IsRefusal.ShouldBeTrue();
    [Fact] void should_not_call_the_chat_client() => chat.DidNotReceiveWithAnyArgs().GetResponseAsync(default!, default, default);
    [Fact] void should_report_the_highest_similarity_across_the_pool() => result.Confidence.ShouldEqual(0.45);
    [Fact] void should_keep_the_rrf_top_passages_as_diagnostics() => result.Passages.SequenceEqual(_all.Take(8)).ShouldBeTrue();
}
