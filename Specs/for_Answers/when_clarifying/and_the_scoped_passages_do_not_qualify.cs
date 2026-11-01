// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_clarifying;

public class and_the_scoped_passages_do_not_qualify : given.a_grounded_answerer
{
    void Establish()
    {
        Found(Passage(0.8));
        passages.Search(Arg.Is<Question>(question => question.Scope == InteractionProduct.Arc), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<Passage>>([Passage(0.4)]));
    }

    async Task Because() => result = await answers.For(new("List all Cratis products") { Scope = InteractionProduct.Arc }, "cli", TestContext.Current.CancellationToken);

    [Fact] void should_retry_unscoped() => passages.Received(1).Search(Arg.Is<Question>(question => question.Scope == null), Arg.Any<int>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_refuse() => result.IsRefusal.ShouldBeFalse();
    [Fact] void should_use_the_qualified_unscoped_passages() => result.Passages.Single().SemanticSimilarity.ShouldEqual(0.8);
}
