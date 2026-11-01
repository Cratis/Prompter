// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_retrieving;

public class and_only_a_weak_summary_matches : given.a_grounded_answerer
{
    void Establish() => Found(
        Passage(0.1) with { IsSummary = true },
        Passage(0.9) with { IsPageExpansion = true });

    async Task Because() => result = await answers.For(new("What are all the non-Cratis products?"), "spec", TestContext.Current.CancellationToken);

    [Fact] void should_refuse_instead_of_promoting_unadmitted_expansion() => result.IsRefusal.ShouldBeTrue();
    [Fact] void should_not_call_chat() => chat.DidNotReceive().GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_cite_a_page() => result.Citations.ShouldBeEmpty();
}
