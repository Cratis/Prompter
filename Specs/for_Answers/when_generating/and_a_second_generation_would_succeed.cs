// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_generating;

public class and_a_second_generation_would_succeed : given.a_grounded_answerer
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

    [Fact] void should_dispatch_once_with_the_output_bound() => chat.Received(1).GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Is<ChatOptions>(value => value.MaxOutputTokens == options.Answering.MaxOutputTokens), Arg.Any<CancellationToken>());
    [Fact] void should_offer_relevant_pages() => result.Text.ShouldEqual("I found relevant pages but couldn't compose an answer. These pages may help.");
    [Fact] void should_not_classify_from_another_generation() => result.Category.ShouldEqual(InteractionCategory.Unknown);
    [Fact] void should_not_identify_a_product_from_another_generation() => result.Product.ShouldEqual(InteractionProduct.OtherOrUnknown);
    [Fact] void should_not_refuse() => result.IsRefusal.ShouldBeFalse();
    [Fact] void should_cite_the_qualified_page() => result.Citations.Single().Value.ShouldEqual("https://cratis.io/chronicle/");
}
