// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_citing;

public class and_passages_share_pages : given.a_grounded_answerer
{
    void Establish()
    {
        Found(
            Passage(0.9) with { Page = new("https://cratis.io/a.md"), Content = "First excerpt" },
            Passage(0.8) with { Page = new("https://cratis.io/b.md"), Content = "Second page" },
            Passage(0.7) with { Page = new("https://cratis.io/a.md"), Content = "Another excerpt" });
        chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            sent = call.Arg<IEnumerable<ChatMessage>>().ToArray();
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Only B [2]. Invalid [0] [4] [999].")));
        });
    }

    async Task Because() => result = await answers.For(new("Q?"), "spec", TestContext.Current.CancellationToken);

    [Fact] void should_group_a_pages_excerpts_before_the_next_page() => sent[1].Text.IndexOf("Another excerpt", StringComparison.Ordinal).ShouldBeLessThan(sent[1].Text.IndexOf("[2]", StringComparison.Ordinal));
    [Fact] void should_number_distinct_pages_only() => sent[1].Text.ShouldNotContain("[3]");
    [Fact] void should_list_only_referenced_pages() => result.Citations.Select(page => page.Value).ShouldEqual(["https://cratis.io/b.md"]);
    [Fact] void should_renumber_sparse_references_and_strip_dangling_markers() => result.Text.ShouldEqual("Only B [1]. Invalid   .");
}
