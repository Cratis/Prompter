// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_citing;

public class and_more_than_four_pages_are_referenced : given.a_grounded_answerer
{
    void Establish()
    {
        Found([.. Enumerable.Range(1, 6).Select(index => Passage(0.8) with { Page = new($"https://cratis.io/page-{index}.md") })]);
        chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponse(new ChatMessage(ChatRole.Assistant, "[6] [2] [1] [5] [4] [3]")));
    }

    async Task Because() => result = await answers.For(new("Q?"), "spec", TestContext.Current.CancellationToken);

    [Fact] void should_keep_every_referenced_page_in_number_order() => result.Citations.Select(page => page.Value).ShouldEqual(Enumerable.Range(1, 6).Select(index => $"https://cratis.io/page-{index}.md"));
    [Fact] void should_preserve_valid_marker_numbers() => result.Text.ShouldEqual("[6] [2] [1] [5] [4] [3]");
}
