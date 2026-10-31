// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_classifying;

public class and_the_question_is_refused_before_generation : given.a_grounded_answerer
{
    void Establish() => Found(Passage(-0.2));

    async Task Because() => result = await answers.For(new("Question?"), "cli", TestContext.Current.CancellationToken);

    [Fact] void should_not_call_the_model() => chat.DidNotReceive().GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_default_the_signals() => (result.Category, result.Product).ShouldEqual((InteractionCategory.Unknown, InteractionProduct.OtherOrUnknown));
    [Fact] void should_record_zero_signals() => interactions.Received(1).Record(Arg.Is<Interaction>(interaction => interaction.Category == InteractionCategory.Unknown && interaction.Product == InteractionProduct.OtherOrUnknown), Arg.Any<CancellationToken>());
}
