// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Retrieval;

namespace Cratis.Prompter.Specs.for_Answers.when_applying_the_retrieval_gate;

public class and_a_lower_ranked_passage_is_grounded : given.a_grounded_answerer
{
    Passage _first = null!;
    Passage _second = null!;

    void Establish()
    {
        _first = Passage(0.6, 1.0 / 62);
        _second = Passage(0.85, 1.0 / 63) with { Page = new("https://cratis.io/chronicle/events/"), Content = "Immutable events are facts." };
        Found(
            Passage(0.2, 2.0 / 61) with { Page = new("https://cratis.io/unrelated/"), Content = "LEXICAL_ONLY_UNRELATED" },
            _first,
            _second,
            Passage(double.NaN) with { Content = "INVALID_SIMILARITY" });
    }

    async Task Because() => result = await answers.For(new("What is Cratis Chronicle?"), "cli");

    [Fact] void should_answer_from_the_lower_ranked_qualified_passages() => result.IsRefusal.ShouldBeFalse();
    [Fact] void should_preserve_rrf_order_rather_than_sort_by_similarity() => result.Passages.SequenceEqual([_first, _second]).ShouldBeTrue();
    [Fact] void should_exclude_weak_context() => sent[1].Text.ShouldNotContain("LEXICAL_ONLY_UNRELATED");
    [Fact] void should_exclude_invalid_context() => sent[1].Text.ShouldNotContain("INVALID_SIMILARITY");
    [Fact] void should_include_the_first_qualified_excerpt() => sent[1].Text.ShouldContain("[1] https://cratis.io/chronicle/ — Overview");
    [Fact] void should_include_the_second_qualified_excerpt() => sent[1].Text.ShouldContain("Immutable events are facts.");
    [Fact] void should_cite_only_qualified_pages() => result.Citations.SequenceEqual([_first.Page, _second.Page]).ShouldBeTrue();
    [Fact] void should_report_the_rrf_first_admitted_similarity() => result.Confidence.ShouldEqual(0.6);
}
