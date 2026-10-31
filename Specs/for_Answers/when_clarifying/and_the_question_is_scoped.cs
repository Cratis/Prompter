// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_clarifying;

public class and_the_question_is_scoped : given.a_grounded_answerer
{
    void Establish()
    {
        Found(new Passage(new("https://cratis.io/chronicle/a/"), "T", "H", "c", 0.02, 0.80), new Passage(new("https://cratis.io/arc/b/"), "T", "H", "c", 0.01, 0.79));
    }

    async Task Because() => result = await answers.For(new("How do I handle commands?") { Scope = InteractionProduct.Arc }, "cli", TestContext.Current.CancellationToken);

    [Fact] void should_pass_the_scope_to_retrieval() => passages.Received(1).Search(Arg.Is<Question>(question => question.Scope == InteractionProduct.Arc), Arg.Any<int>(), Arg.Any<CancellationToken>());
    [Fact] void should_generate_an_answer() => result.Clarification.ShouldBeNull();
}
