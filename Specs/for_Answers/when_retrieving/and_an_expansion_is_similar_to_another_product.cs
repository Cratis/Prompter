// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Prompter.Specs.for_Answers.when_retrieving;

public class and_an_expansion_is_similar_to_another_product : given.a_grounded_answerer
{
    void Establish() => Found(
        Passage(0.9),
        Passage(0.8) with { Page = new("https://cratis.io/arc/"), Title = "Arc" },
        Passage(0.89) with { Page = new("https://cratis.io/arc/"), Title = "Arc", HeadingPath = "Arc > Detail", Content = "Arc detail.", ChunkId = "https://cratis.io/arc/#3", IsSectionExpansion = true });

    async Task Because() => result = await answers.For(new("How do I store events?") { AllowClarification = true }, "spec", TestContext.Current.CancellationToken);

    [Fact] void should_not_ask_which_product_because_of_an_expansion() => result.Clarification.ShouldBeNull();
    [Fact] void should_still_answer() => result.IsRefusal.ShouldBeFalse();
}
