// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_synthesizing;

public class and_two_pages_answer_together : given.a_grounded_answerer
{
    void Establish()
    {
        options.Answering.MaxPassages = 6;
        Found(Passage(0.96) with { Page = new("https://cratis.studio/") }, Passage(0.95), Passage(0.94) with { Page = new("https://cratis.io/arc/") }, Passage(0.93) with { Page = new("https://cratis.direct/") }, Passage(0.92) with { Page = new("https://cratis.no/about/") }, Passage(0.91));
        chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            sent = call.Arg<IEnumerable<ChatMessage>>().ToArray();
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, "Use the stack (inferred from [1] and [5])."));
        });
    }
    async Task Because() => result = await answers.For(new("Which free tools implement a Studio model?"), "spec");
    [Fact] void should_not_refuse() => result.IsRefusal.ShouldBeFalse();
    [Fact] void should_cite_every_page_relied_on() => result.Citations.ShouldEqual([new PageUrl("https://cratis.studio/"), new("https://cratis.no/about/")]);
    [Fact] void should_send_both_excerpts() => sent[1].Text.ShouldContain("[5] https://cratis.no/about/");
    [Fact] void should_keep_the_first_excerpt() => sent[1].Text.ShouldContain("[1] https://cratis.studio/");
}
