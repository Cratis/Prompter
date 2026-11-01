// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_citing;

public class and_fence_delimiters_vary : given.a_grounded_answerer
{
    const string Generated = """
        Before [5].
        ````markdown
        ```csharp
        [1] items[0] [99]
        ```
        `````
        After `literal [1]` [2].
        ```unfinished
        [1] [2] [99]
        """;

    void Establish()
    {
        Found([.. Enumerable.Range(1, 5).Select(index => Passage(0.8) with { Page = new($"https://cratis.io/page-{index}.md") })]);
        chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponse(new ChatMessage(ChatRole.Assistant, Generated)));
    }

    async Task Because() => result = await answers.For(new("Q?"), "spec", TestContext.Current.CancellationToken);

    [Fact] void should_not_close_a_fence_at_a_shorter_embedded_delimiter() => result.Text.ShouldContain("````markdown\n```csharp\n[1] items[0] [99]\n```\n`````");
    [Fact] void should_recognize_prose_after_the_longer_closing_fence() => result.Text.ShouldContain("After `literal [1]` [1].");
    [Fact] void should_protect_the_remainder_of_an_unfinished_fence() => result.Text.ShouldContain("```unfinished\n[1] [2] [99]");
    [Fact] void should_ignore_all_code_only_page_references() => result.Citations.Select(page => page.Value).ShouldEqual(["https://cratis.io/page-2.md", "https://cratis.io/page-5.md"]);
}
