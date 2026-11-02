// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_Answers.when_retrieving;

public class and_section_expansion_is_below_the_threshold : given.a_grounded_answerer
{
    void Establish() => Found(
        Passage(0.9),
        Passage(0.2) with { HeadingPath = "Chronicle > Related", Content = "Weakly similar section text", ChunkId = "https://cratis.io/chronicle/#3", IsSectionExpansion = true });

    async Task Because() => result = await answers.For(new("What is Chronicle good at?"), "spec", TestContext.Current.CancellationToken);

    [Fact] void should_include_the_row() => sent[1].Text.ShouldContain("Weakly similar section text");
    [Fact] void should_not_refuse() => result.IsRefusal.ShouldBeFalse();
}
