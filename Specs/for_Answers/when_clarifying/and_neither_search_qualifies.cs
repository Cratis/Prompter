// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_clarifying;

public class and_neither_search_qualifies : given.a_grounded_answerer
{
    void Establish() => Found(Passage(0.4));

    async Task Because() => result = await answers.For(new("Question?") { Scope = InteractionProduct.Arc }, "cli", TestContext.Current.CancellationToken);

    [Fact] void should_search_twice_only() => passages.Received(2).Search(Arg.Any<Question>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    [Fact] void should_keep_the_retrieval_refusal() => result.IsRefusal.ShouldBeTrue();
    [Fact] void should_not_generate() => chat.DidNotReceive().GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>());
}
