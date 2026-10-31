// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_classifying;

public class and_the_model_returns_an_envelope : given.a_grounded_answerer
{
    void Establish()
    {
        Found(Passage(0.8));
        chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, """{"text":"Not supported yet [1].","category":"missing-capability","product":"Chronicle"}"""))));
    }

    async Task Because() => result = await answers.For(new("Can Chronicle do X?"), "cli", TestContext.Current.CancellationToken);

    [Fact] void should_render_only_the_text() => result.Text.ShouldEqual("Not supported yet [1].");
    [Fact] void should_expose_the_category() => result.Category.ShouldEqual(InteractionCategory.MissingCapability);
    [Fact] void should_expose_the_product() => result.Product.ShouldEqual(InteractionProduct.Chronicle);
    [Fact] void should_generate_exactly_once() => chat.Received(1).GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_request_the_structured_format() => chat.Received(1).GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Is<ChatOptions>(options => options.ResponseFormat == AnswerGeneration.Format), Arg.Any<CancellationToken>());
    [Fact] void should_record_the_classification() => interactions.Received(1).Record(Arg.Is<Interaction>(interaction => interaction.Category == InteractionCategory.MissingCapability && interaction.Product == InteractionProduct.Chronicle), Arg.Any<CancellationToken>());
    [Fact] void should_keep_the_citations() => result.Citations.Count().ShouldEqual(1);
    [Fact] void should_not_become_a_refusal() => result.IsRefusal.ShouldBeFalse();
}
