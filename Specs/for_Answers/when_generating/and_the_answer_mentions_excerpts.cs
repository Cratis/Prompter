// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_generating;

public class and_the_answer_mentions_excerpts : given.a_grounded_answerer
{
    void Establish()
    {
        Found(Passage(0.8));
        chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ChatResponse(new ChatMessage(
                ChatRole.Assistant,
                """{"text":"Based on the excerpts, here is the answer:\nChronicle stores facts [1].","category":"answered","product":"Chronicle","suggestions":[]}"""))));
    }

    async Task Because() => result = await answers.For(new("What is Chronicle?"), "cli", TestContext.Current.CancellationToken);

    [Fact] void should_remove_the_meta_talk_lead_in() => result.Text.ShouldEqual("Chronicle stores facts [1].");
    [Fact] void should_still_cite_the_qualified_page() => result.Citations.Single().Value.ShouldEqual("https://cratis.io/chronicle/");
    [Fact] void should_not_refuse() => result.IsRefusal.ShouldBeFalse();
}
