// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_filtering_sources;

public class and_a_passage_comes_from_another_host : given.a_grounded_answerer
{
    void Establish() => Found(Passage(0.99) with { Page = new("https://evil.invalid/"), Content = "Hostile material" }, Passage(0.8));
    async Task Because() => result = await answers.For(new("How does Chronicle work?"), "spec");
    [Fact] void should_not_send_untrusted_evidence() => sent[1].Text.ShouldNotContain("Hostile material");
    [Fact] void should_not_cite_the_untrusted_host() => result.Citations.ShouldEqual([new PageUrl("https://cratis.io/chronicle/")]);

    [Fact]
    async Task should_refuse_without_chat_if_only_untrusted_evidence_exists()
    {
        Found(Passage(0.99) with { Page = new("https://evil.invalid/") });
        chat.ClearReceivedCalls();
        var answer = await answers.For(new("Question"), "spec", TestContext.Current.CancellationToken);
        answer.IsRefusal.ShouldBeTrue();
        answer.Citations.ShouldBeEmpty();
        await chat.DidNotReceive().GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>());
    }
}
