// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Cratis.Prompter.Retrieval;
using Cratis.Prompter.Storage;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_generating;

public class and_the_response_is_truncated_inside_text : given.a_grounded_answerer
{
    void Establish()
    {
        Found(
            Passage(0.8),
            Passage(0.75),
            new Passage(new("https://cratis.io/arc/"), "Arc", "Overview", "CQRS", 0.02, 0.7),
            new Passage(new("https://cratis.io/components/"), "Components", "Overview", "UI", 0.01, 0.7),
            new Passage(new("https://cratis.io/cli/"), "CLI", "Overview", "Tools", 0.005, 0.7),
            new Passage(new("https://cratis.io/stage/"), "Stage", "Overview", "Modeling", 0.004, 0.7));
        chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "{\"text\":\"Chronicle is")) { FinishReason = ChatFinishReason.Length }));
    }

    async Task Because() => result = await answers.For(new("What are all Cratis products?"), "discord", TestContext.Current.CancellationToken);

    [Fact] void should_retry_exactly_once() => chat.Received(2).GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_claim_a_lack_of_grounding() => result.IsRefusal.ShouldBeFalse();
    [Fact] void should_explain_the_composition_failure() => result.Text.ShouldEqual("I found relevant pages but couldn't compose an answer. These pages may help.");
    [Fact] void should_offer_the_top_four_distinct_pages() => result.Citations.Select(page => page.Value).ShouldEqual(["https://cratis.io/chronicle/", "https://cratis.io/arc/", "https://cratis.io/components/", "https://cratis.io/cli/"]);
    [Fact] void should_include_citation_provenance() => result.CitationProvenance.Count.ShouldEqual(4);
    [Fact] void should_preserve_qualified_confidence() => result.Confidence.ShouldEqual(0.8);
    [Fact] void should_record_a_single_non_refusal_interaction() => interactions.Received(1).Record(Arg.Is<Interaction>(interaction => !interaction.WasRefusal), Arg.Any<CancellationToken>());
    [Fact] void should_attach_the_interaction_id() => result.InteractionId.ShouldEqual(42L);
}
