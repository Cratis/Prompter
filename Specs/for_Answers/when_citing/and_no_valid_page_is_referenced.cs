// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prompter.Answering;
using Microsoft.Extensions.AI;
using NSubstitute;

namespace Cratis.Prompter.Specs.for_Answers.when_citing;

public class and_no_valid_page_is_referenced : given.a_grounded_answerer
{
    void Establish()
    {
        Found([.. Enumerable.Range(1, 6).Select(index => Passage(0.8) with { Page = new($"https://cratis.io/page-{index}.md") })]);
        chat.GetResponseAsync(Arg.Any<IEnumerable<ChatMessage>>(), Arg.Any<ChatOptions>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Unmarked answer. Invalid [0][7][99999999999999999999].")));
    }

    async Task Because() => result = await answers.For(new("Q?"), "spec", TestContext.Current.CancellationToken);

    [Fact] void should_strip_invalid_and_overflowed_marker_numbers() => result.Text.ShouldEqual("Unmarked answer. Invalid .");
    [Fact] void should_preserve_the_top_four_page_fallback() => result.Citations.Select(page => page.Value).ShouldEqual(Enumerable.Range(1, 4).Select(index => $"https://cratis.io/page-{index}.md"));
}
