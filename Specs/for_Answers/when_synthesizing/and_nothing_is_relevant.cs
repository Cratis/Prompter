// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_synthesizing;

public class and_nothing_is_relevant : given.a_grounded_answerer
{
    void Establish() => Found(Passage(0.49), Passage(0.45));
    async Task Because() => result = await answers.For(new("Question"), "spec");
    [Fact] void should_refuse() => result.IsRefusal.ShouldBeTrue();
    [Fact] void should_not_cite() => result.Citations.ShouldBeEmpty();
    [Fact] async Task should_never_call_chat() => await chat.DidNotReceive().GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>());
}
