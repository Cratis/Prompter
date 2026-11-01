// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_clarifying;

public class and_the_scope_matches_nothing : given.a_grounded_answerer
{
    void Establish()
    {
        Found(Passage(0.8));
        passages.Search(Arg.Is<Question>(question => question.Scope == InteractionProduct.Arc), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<Passage>>([]));
    }

    async Task Because() => result = await answers.For(new("What are all Cratis products?") { Scope = InteractionProduct.Arc, AllowClarification = true }, "discord-clarification", TestContext.Current.CancellationToken);

    [Fact] void should_try_the_scope_first() => passages.Received(1).Search(Arg.Is<Question>(question => question.Scope == InteractionProduct.Arc), Arg.Any<int>(), Arg.Any<CancellationToken>());
    [Fact] void should_retry_the_same_question_unscoped() => passages.Received(1).Search(Arg.Is<Question>(question => question.Scope == null && question.Value == "What are all Cratis products?"), Arg.Any<int>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_refuse() => result.IsRefusal.ShouldBeFalse();
    [Fact] void should_not_ask_for_clarification_again() => result.Clarification.ShouldBeNull();
    [Fact] void should_generate_once() => chat.Received(1).GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_use_unscoped_confidence() => result.Confidence.ShouldEqual(0.8);
}
