// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_retrieving;

public class and_only_section_expansion_matches : given.a_grounded_answerer
{
    void Establish() => Found(
        Passage(0.1),
        Passage(0.8) with { ChunkId = "https://cratis.io/chronicle/#3", IsSectionExpansion = true });

    async Task Because() => result = await answers.For(new("What is Chronicle good at?"), "spec", TestContext.Current.CancellationToken);

    [Fact] void should_refuse() => result.IsRefusal.ShouldBeTrue();
    [Fact] void should_not_call_the_model() => chat.ReceivedCalls().Any().ShouldBeFalse();
}
