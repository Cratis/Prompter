// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_generating;

/// <summary>
/// v0.14.0 regression: a product list longer than the output cap was replaced by the generic
/// "couldn't compose" text, so the answer named no product at all.
/// </summary>
public class and_the_output_cap_cuts_a_product_list : given.a_grounded_answerer
{
    void Establish()
    {
        Found(
            Passage(0.8),
            new Passage(new("https://cratis.io/arc/"), "Arc", "Overview", "CQRS", 0.02, 0.7),
            new Passage(new("https://cratis.io/components/"), "Components", "Overview", "UI", 0.01, 0.7));
        chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse(new ChatMessage(
                ChatRole.Assistant,
                "{\"text\":\"- Chronicle: event store [1]\\n- Components: React UI [3]\\n- Arc: CQRS [2]\\n- Fundamentals: shar")) { FinishReason = ChatFinishReason.Length }));
    }

    async Task Because() => result = await answers.For(new("What are all Cratis products and what do they do?"), "eval", TestContext.Current.CancellationToken);

    [Fact] void should_answer_with_the_complete_lines() => result.Text.ShouldEqual("- Chronicle: event store [1]\n- Components: React UI [3]\n- Arc: CQRS [2]\n\u2026");
    [Fact] void should_cite_the_referenced_pages_in_order() => result.Citations.Select(page => page.Value).ShouldEqual(["https://cratis.io/chronicle/", "https://cratis.io/arc/", "https://cratis.io/components/"]);
    [Fact] void should_not_refuse() => result.IsRefusal.ShouldBeFalse();
    [Fact] void should_record_a_single_non_refusal_interaction() => interactions.Received(1).Record(Arg.Is<Interaction>(interaction => !interaction.WasRefusal), Arg.Any<CancellationToken>());
}
