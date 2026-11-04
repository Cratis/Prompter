// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_refusing_out_of_scope;

public class and_the_model_reports_a_docs_gap : given.a_grounded_answerer
{
    void Establish()
    {
        Found(Passage(0.55));
        chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, """{"text":"The docs do not cover this [1].","category":"docs-gap","product":"Chronicle"}"""))));
    }

    async Task Because() => result = await answers.For(new("How do I center a div?"), "cli", TestContext.Current.CancellationToken);

    [Fact] void should_not_refuse() => result.IsRefusal.ShouldBeFalse();
    [Fact] void should_keep_the_citations() => result.Citations.Count().ShouldEqual(1);
    [Fact] void should_expose_the_category() => result.Category.ShouldEqual(InteractionCategory.DocsGap);
}
