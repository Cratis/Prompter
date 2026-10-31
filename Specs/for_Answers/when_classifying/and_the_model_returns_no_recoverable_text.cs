// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_classifying;

public class and_the_model_returns_no_recoverable_text : given.a_grounded_answerer
{
    void Establish()
    {
        Found(Passage(0.8));
        chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, """{"category":"answered","product":"Arc"}"""))));
    }

    async Task Because() => result = await answers.For(new("Question?"), "cli", TestContext.Current.CancellationToken);

    [Fact] void should_take_the_honest_refusal_path() => result.IsRefusal.ShouldBeTrue();
    [Fact] void should_not_leak_the_envelope() => result.Text.ShouldNotContain("category");
    [Fact] void should_not_generate_again() => chat.Received(1).GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_record_unknown_signals() => interactions.Received(1).Record(Arg.Is<Interaction>(interaction => interaction.Category == InteractionCategory.Unknown && interaction.Product == InteractionProduct.OtherOrUnknown), Arg.Any<CancellationToken>());
}
