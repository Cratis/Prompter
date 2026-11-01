// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_generating;

public class and_the_retry_is_parseable : given.a_grounded_answerer
{
    void Establish()
    {
        Found(Passage(0.8));
        chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "{\"text\":\"Chronicle is"))),
                Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, """{"text":"Chronicle stores facts [1].","category":"answered","product":"Chronicle","suggestions":[]}"""))));
    }

    async Task Because() => result = await answers.For(new("What is Chronicle?"), "cli", TestContext.Current.CancellationToken);

    [Fact] void should_retry_once_with_the_same_output_bound() => chat.Received(2).GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Is<ChatOptions>(value => value.MaxOutputTokens == options.Answering.MaxOutputTokens), Arg.Any<CancellationToken>());
    [Fact] void should_use_the_retry_text() => result.Text.ShouldEqual("Chronicle stores facts [1].");
    [Fact] void should_use_the_retry_category() => result.Category.ShouldEqual(InteractionCategory.Answered);
    [Fact] void should_use_the_retry_product() => result.Product.ShouldEqual(InteractionProduct.Chronicle);
    [Fact] void should_not_refuse() => result.IsRefusal.ShouldBeFalse();
    [Fact] void should_cite_the_qualified_page() => result.Citations.Single().Value.ShouldEqual("https://cratis.io/chronicle/");
}
