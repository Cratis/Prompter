// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_Answers.when_retrieving;

public class and_a_section_expands_within_budget : given.a_grounded_answerer
{
    void Establish() => Found(
        Passage(0.9) with { HeadingPath = "Chronicle > Strengths", Content = "Anchor text", ChunkId = "https://cratis.io/chronicle/#2" },
        Passage(0.1) with { HeadingPath = "Chronicle > Strengths > Later", Content = "Later section text", ChunkId = "https://cratis.io/chronicle/#4", IsSectionExpansion = true },
        Passage(0.1) with { HeadingPath = "Chronicle > Strengths > Events", Content = "Earlier section text", ChunkId = "https://cratis.io/chronicle/#3", IsSectionExpansion = true });

    async Task Because() => result = await answers.For(new("What is Chronicle good at?"), "spec", TestContext.Current.CancellationToken);

    [Fact] void should_send_the_section_rows() => sent[1].Text.ShouldContain("Earlier section text");
    [Fact] void should_send_them_in_chunk_order() => (sent[1].Text.IndexOf("Anchor text") < sent[1].Text.IndexOf("Earlier section text") && sent[1].Text.IndexOf("Earlier section text") < sent[1].Text.IndexOf("Later section text")).ShouldBeTrue();
    [Fact] void should_keep_the_citation_to_the_page() => result.Citations.Select(page => page.Value).ShouldEqual(["https://cratis.io/chronicle/"]);
    [Fact] void should_not_count_them_as_candidates() => result.Passages.Count(passage => passage.IsSectionExpansion).ShouldEqual(2);
}
