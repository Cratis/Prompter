// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_clarifying;

public class and_two_products_tie : given.a_grounded_answerer
{
    void Establish()
    {
        Found(new Passage(new("https://cratis.io/chronicle/a/"), "T", "H", "c", 0.02, 0.80), new Passage(new("https://cratis.io/arc/b/"), "T", "H", "c", 0.01, 0.79));
    }

    async Task Because() => result = await answers.For(new("How do I handle commands?"), "cli", TestContext.Current.CancellationToken);

    [Fact] void should_not_call_the_model() => chat.DidNotReceive().GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_record_the_interaction() => interactions.DidNotReceive().Record(Arg.Any<Interaction>(), Arg.Any<CancellationToken>());
    [Fact] void should_name_both_products() => (result.Text.Contains("Chronicle") && result.Text.Contains("Arc")).ShouldBeTrue();
    [Fact] void should_carry_the_clarification() => result.Clarification.ShouldNotBeNull();
    [Fact] void should_have_no_interaction_id() => result.InteractionId.ShouldBeNull();
}
